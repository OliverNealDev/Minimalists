using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This AIManager's strategy is centered around launching powerful, **Coordinated Attacks**.
/// While it still handles defense and economic upgrades, its primary goal is to identify a key
/// enemy target and overwhelm it by sending units from multiple source nodes at once.
/// Its priority is:
/// 1.  **Critical Defense:** Survive at all costs.
/// 2.  **Coordinated Attack:** Launch a major, multi-pronged offensive if an opportunity exists.
/// 3.  **Economic Upgrades:** Strengthen its unit production.
/// 4.  **Single Attacks / Expansion:** Pick off weak targets or unclaimed nodes as a lower priority.
/// 5.  **Consolidation:** Reposition forces during lulls in the action.
/// </summary>
public class AIManager3 : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 1.0f;

    // --- AI Tuning Parameters ---
    private const int COORDINATED_ATTACK_MIN_UNITS = 70; // Total units the AI must have before attempting a coordinated attack.
    private const int COORDINATED_ATTACK_ADVANTAGE = 20; // Required surplus of units to launch the attack.
    private const int DEFENSE_SAFETY_MARGIN = 10;        // How many units a node should keep for itself after sending reinforcements.
    private const int SINGLE_ATTACK_ADVANTAGE = 5;       // Required advantage for a standard 1-on-1 attack.
    private const int NODE_RESERVE_UNITS = 8;            // Units to leave behind in a node after an attack.

    void Start()
    {
        if (GameManager.Instance == null || aiFaction == null)
        {
            this.enabled = false;
            return;
        }
        StartCoroutine(MakeDecisionsRoutine());
    }

    private IEnumerator MakeDecisionsRoutine()
    {
        yield return new WaitForSeconds(3.0f);

        while (true)
        {
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                ExecuteStrategicAction();
            }
            yield return new WaitForSeconds(DECISION_DELAY);
        }
    }

    /// <summary>
    /// Executes the AI's logic based on a clear priority hierarchy.
    /// </summary>
    private void ExecuteStrategicAction()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any()) return;

        // **Priority 1: Defense.**
        if (PerformDefensiveAction(myNodes)) return;

        // **Priority 2: Coordinated Attack.** The signature move of this AI.
        if (PerformCoordinatedAttack(myNodes)) return;

        // **Priority 3: Upgrades.**
        if (PerformUpgradeAction(myNodes)) return;
        
        // **Priority 4: Single-Node Attacks & Expansion.**
        if (PerformSingleAttackAction(myNodes)) return;

        // **Priority 5: Consolidation.**
        PerformConsolidationAction(myNodes);
    }

    // ## Decision-Making Methods ##
    // =============================

    /// <summary>
    /// **DEFENSE:** Finds and reinforces the most threatened friendly node.
    /// </summary>
    private bool PerformDefensiveAction(List<ConstructController> myNodes)
    {
        ConstructController mostThreatenedNode = null;
        int highestThreat = 0;

        foreach (var myNode in myNodes)
        {
            int incomingThreat = GameManager.Instance.allUnits
                .Count(unit => unit.owner != aiFaction && unit.target == myNode);
            
            int threatLevel = incomingThreat - myNode.UnitCount;

            if (threatLevel > highestThreat)
            {
                highestThreat = threatLevel;
                mostThreatenedNode = myNode;
            }
        }

        if (mostThreatenedNode != null)
        {
            // Find the best nodes to send reinforcements from.
            var bestReinforcer = myNodes
                .Where(n => n != mostThreatenedNode && n.UnitCount > highestThreat + DEFENSE_SAFETY_MARGIN)
                .OrderBy(n => Vector3.Distance(n.transform.position, mostThreatenedNode.transform.position))
                .FirstOrDefault();

            if (bestReinforcer != null)
            {
                bestReinforcer.SendExactUnits(mostThreatenedNode, highestThreat + 5);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// **COORDINATED ATTACK:** Identifies a high-value target and attacks it from multiple nodes at once.
    /// </summary>
    private bool PerformCoordinatedAttack(List<ConstructController> myNodes)
    {
        // Don't attempt this complex maneuver without sufficient forces.
        if (myNodes.Sum(n => n.UnitCount) < COORDINATED_ATTACK_MIN_UNITS) return false;
        
        var enemyNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction)
            .ToList();
        
        if (!enemyNodes.Any()) return false;

        // Find the most valuable enemy target (e.g., the one with the most units).
        var bestTarget = enemyNodes.OrderByDescending(n => n.UnitCount).FirstOrDefault();
        if (bestTarget == null) return false;
        
        // Find all of our nodes that can contribute to the attack.
        var contributingAttackers = myNodes
            .Where(n => n.UnitCount > NODE_RESERVE_UNITS)
            .OrderBy(n => Vector3.Distance(n.transform.position, bestTarget.transform.position))
            .ToList();

        int availableForce = contributingAttackers.Sum(n => n.UnitCount - NODE_RESERVE_UNITS);
        
        // Check if we have overwhelming force for the coordinated strike.
        if (availableForce > bestTarget.UnitCount + COORDINATED_ATTACK_ADVANTAGE)
        {
            // Attack is a go! Send units from all contributing nodes.
            foreach (var attacker in contributingAttackers)
            {
                int unitsToSend = attacker.UnitCount - NODE_RESERVE_UNITS;
                if (unitsToSend > 0)
                {
                    attacker.SendExactUnits(bestTarget, unitsToSend);
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// **UPGRADES:** Invests in the most cost-effective upgrades, prioritizing houses.
    /// </summary>
    private bool PerformUpgradeAction(List<ConstructController> myNodes)
    {
        var bestNodeToUpgrade = myNodes
            .Where(n => n.currentConstructData.upgradedVersion != null && n.UnitCount >= n.currentConstructData.upgradeCost)
            .OrderByDescending(n => n.currentConstructData is HouseData) // Prioritize houses first
            .ThenByDescending(n => n.UnitCount) // Then the one with the most units
            .FirstOrDefault();

        if (bestNodeToUpgrade != null)
        {
            bestNodeToUpgrade.AttemptUpgrade();
            return true;
        }

        return false;
    }

    /// <summary>
    /// **SINGLE ATTACK / EXPANSION:** A fallback for smaller, opportunistic attacks.
    /// </summary>
    private bool PerformSingleAttackAction(List<ConstructController> myNodes)
    {
        var allTargets = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction).ToList();
        if (!allTargets.Any()) return false;

        ConstructController bestSource = null;
        ConstructController bestTarget = null;
        float bestScore = -1;

        foreach (var source in myNodes)
        {
            if (source.UnitCount < SINGLE_ATTACK_ADVANTAGE + NODE_RESERVE_UNITS) continue;
            
            foreach (var target in allTargets)
            {
                if (source.UnitCount > target.UnitCount + SINGLE_ATTACK_ADVANTAGE)
                {
                    // Score favors closer, weaker targets. Neutrals are highly valued.
                    float distance = Vector3.Distance(source.transform.position, target.transform.position);
                    float score = 100f / (distance * (target.UnitCount + 1));
                    if (target.Owner == GameManager.Instance.unclaimedFaction)
                    {
                        score *= 2.0f; // Strongly prioritize capturing neutral buildings.
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSource = source;
                        bestTarget = target;
                    }
                }
            }
        }

        if (bestSource != null)
        {
            bestSource.SendUnits(bestTarget, 0.75f); // Send a significant portion of units.
            return true;
        }

        return false;
    }

    /// <summary>
    /// **CONSOLIDATION:** Moves units from safe rear nodes to the front line.
    /// </summary>
    private bool PerformConsolidationAction(List<ConstructController> myNodes)
    {
        if (myNodes.Count < 2) return false;
        
        var richestNode = myNodes.OrderByDescending(n => n.UnitCount).FirstOrDefault();
        if (richestNode == null || richestNode.UnitCount < 30) return false;
        
        var enemies = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction).ToList();
        if (!enemies.Any()) return false;

        // Find a "forward" node to reinforce.
        var forwardNode = myNodes
            .OrderBy(n => enemies.Min(e => Vector3.Distance(n.transform.position, e.transform.position)))
            .FirstOrDefault();

        if (forwardNode != null && richestNode != forwardNode)
        {
            richestNode.SendUnits(forwardNode, 0.5f);
            return true;
        }

        return false;
    }
}