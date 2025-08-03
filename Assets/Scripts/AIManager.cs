using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This AIManager implements a high-skill, priority-based decision-making process.
/// It evaluates the game state by prioritizing actions in the following order:
/// 1.  **Defense:** Counter any immediate and overwhelming threats to its constructs.
/// 2.  **Upgrades:** Invest in economic and military superiority by upgrading key constructs.
/// 3.  **Conversions:** Adapt its strategy by converting constructs to specialized types like Turrets.
/// 4.  **Offense/Expansion:** Aggressively capture neutral constructs and attack weak enemy positions.
/// 5.  **Consolidation:** Reposition units to prepare for future attacks if no other actions are available.
/// This approach ensures the AI is reactive, strategic, and efficient.
/// </summary>
public class AIManager : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 0.75f; // AI thinks faster than the example for better reaction time.

    // --- Thresholds & AI Personality ---
    private const int MIN_UNITS_TO_ATTACK_FROM = 15; // Minimum units a construct must have to initiate an attack.
    private const int DEFENSIVE_REINFORCE_MARGIN = 5; // Send this many extra units when defending.
    private const int OFFENSIVE_ATTACK_MARGIN = 8;    // Required unit advantage to launch an attack.
    private const float HOUSE_TO_TURRET_RATIO = 4.0f; // Aim for one turret for every four houses.

    void Start()
    {
        if (GameManager.Instance == null || aiFaction == null)
        {
            Debug.LogError("AIManager is not properly configured. Disabling script.", this);
            this.enabled = false;
            return;
        }
        // Start the main decision-making loop.
        StartCoroutine(MakeDecisionsRoutine());
    }

    private IEnumerator MakeDecisionsRoutine()
    {
        // A brief delay at the start of the match to allow initial game setup.
        yield return new WaitForSeconds(2.0f);

        while (true)
        {
            // Only make decisions if the game is actively being played.
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                ExecuteStrategicAction();
            }
            yield return new WaitForSeconds(DECISION_DELAY);
        }
    }

    /// <summary>
    /// The core of the AI's logic. It calls decision methods based on a fixed priority.
    /// If an action is taken (method returns true), it skips the lower-priority actions for this cycle.
    /// </summary>
    private void ExecuteStrategicAction()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any()) return; // If AI has no nodes, it can't do anything.

        // **Priority 1: Urgent Defense.** Counter immediate threats.
        if (PerformDefensiveAction(myNodes)) return;

        // **Priority 2: Strategic Upgrades.** Strengthen the economy and military.
        if (PerformUpgradeAction(myNodes)) return;

        // **Priority 3: Strategic Conversions.** Adapt node types to the situation.
        if (PerformConversionAction(myNodes)) return;
        
        // **Priority 4: Offensive Maneuvers.** Expand territory and attack enemies.
        if (PerformOffensiveAction(myNodes)) return;

        // **Priority 5: Force Consolidation.** Prepare for the next move if idle.
        PerformConsolidationAction(myNodes);
    }

    // ## Decision-Making Methods ##
    // =============================

    /// <summary>
    /// **DEFENSE:** Identifies the most threatened friendly construct and reinforces it.
    /// A threat is determined by calculating the net unit balance after accounting for all incoming units.
    /// </summary>
    private bool PerformDefensiveAction(List<ConstructController> myNodes)
    {
        ConstructController mostThreatenedNode = null;
        float highestThreat = 0;

        foreach (var myNode in myNodes)
        {
            // Calculate total incoming enemy strength.
            int incomingEnemyUnits = GameManager.Instance.allUnits
                .Count(unit => unit.owner != aiFaction && unit.GetComponent<UnitController>().target == myNode);

            // A simple threat level calculation.
            float threatLevel = incomingEnemyUnits - myNode.UnitCount;

            if (threatLevel > highestThreat)
            {
                highestThreat = threatLevel;
                mostThreatenedNode = myNode;
            }
        }

        // If a significant threat is detected, send reinforcements.
        if (mostThreatenedNode != null && highestThreat > 0)
        {
            // Find the best node to send reinforcements from (closest with enough units).
            var potentialReinforcers = myNodes
                .Where(n => n != mostThreatenedNode && n.UnitCount > highestThreat + DEFENSIVE_REINFORCE_MARGIN)
                .OrderBy(n => Vector3.Distance(n.transform.position, mostThreatenedNode.transform.position));

            var bestReinforcer = potentialReinforcers.FirstOrDefault();

            if (bestReinforcer != null)
            {
                // Send just enough units to counter the threat plus a small margin.
                int unitsToSend = (int)highestThreat + DEFENSIVE_REINFORCE_MARGIN;
                bestReinforcer.SendExactUnits(mostThreatenedNode, unitsToSend);
                return true; // Defensive action taken.
            }
        }

        return false; // No critical defensive action was needed.
    }

    /// <summary>
    /// **UPGRADES:** Finds the most valuable upgrade available and executes it.
    /// Prioritizes upgrades that provide the best return on investment (e.g., increased unit generation).
    /// </summary>
    private bool PerformUpgradeAction(List<ConstructController> myNodes)
    {
        ConstructController bestNodeToUpgrade = null;
        float bestUpgradeROI = 0f; // Return on Investment

        // Find all nodes that are eligible for an upgrade.
        var upgradeableNodes = myNodes.Where(n =>
            n.currentConstructData.upgradedVersion != null &&
            n.UnitCount >= n.currentConstructData.upgradeCost);

        foreach (var node in upgradeableNodes)
        {
            var currentData = node.currentConstructData as HouseData;
            var nextData = node.currentConstructData.upgradedVersion as HouseData;

            // Only calculate ROI for houses, as they drive the economy.
            if (currentData != null && nextData != null)
            {
                float roi = (nextData.unitsPerSecond - currentData.unitsPerSecond) / currentData.upgradeCost;
                if (roi > bestUpgradeROI)
                {
                    bestUpgradeROI = roi;
                    bestNodeToUpgrade = node;
                }
            }
        }

        if (bestNodeToUpgrade != null)
        {
            bestNodeToUpgrade.AttemptUpgrade();
            return true; // Upgrade action taken.
        }
        
        // As a fallback, if no ROI calculation was made (e.g., only non-houses can be upgraded),
        // upgrade the one with the most units.
        var fallbackNode = upgradeableNodes.OrderByDescending(n => n.UnitCount).FirstOrDefault();
        if (fallbackNode != null)
        {
            fallbackNode.AttemptUpgrade();
            return true;
        }

        return false; // No upgrade action was taken.
    }

    /// <summary>
    /// **CONVERSIONS:** Maintains a healthy ratio of Houses to Turrets.
    /// If there are too many houses, it converts a forward-facing house into a turret for defense.
    /// </summary>
    private bool PerformConversionAction(List<ConstructController> myNodes)
    {
        int houseCount = myNodes.Count(n => n.currentConstructData is HouseData);
        int turretCount = myNodes.Count(n => n.currentConstructData is TurretData);

        // Check if we need more turrets based on the desired ratio.
        if (houseCount > 0 && (turretCount == 0 || (float)houseCount / turretCount > HOUSE_TO_TURRET_RATIO))
        {
            var enemies = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction).ToList();
            if (!enemies.Any()) return false;

            // Find a house that is close to an enemy to convert it into a defensive turret.
            var candidateToConvert = myNodes
                .Where(n => n.currentConstructData is HouseData && n.UnitCount >= n.Turret1Data.conversionCost)
                .OrderBy(n => enemies.Min(e => Vector3.Distance(n.transform.position, e.transform.position)))
                .FirstOrDefault();

            if (candidateToConvert != null)
            {
                candidateToConvert.AttemptConvertConstruct(candidateToConvert.Turret1Data);
                return true; // Conversion action taken.
            }
        }
        return false; // No conversion was needed.
    }


    /// <summary>
    /// **OFFENSE:** Finds the best possible attack or expansion target.
    /// It scores potential targets based on value, distance, and required strength, then executes the best option.
    /// </summary>
    private bool PerformOffensiveAction(List<ConstructController> myNodes)
    {
        var targets = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction).ToList();
        if (!targets.Any()) return false;

        ConstructController bestSourceNode = null;
        ConstructController bestTargetNode = null;
        float bestAttackScore = float.MinValue;

        // Find potential attackers that meet the minimum unit requirement.
        var potentialAttackers = myNodes.Where(n => n.UnitCount > MIN_UNITS_TO_ATTACK_FROM);

        foreach (var sourceNode in potentialAttackers)
        {
            foreach (var targetNode in targets)
            {
                int requiredUnits = targetNode.UnitCount + OFFENSIVE_ATTACK_MARGIN;
                
                // Check if the source node has enough units for a successful attack.
                if (sourceNode.UnitCount > requiredUnits)
                {
                    float distance = Vector3.Distance(sourceNode.transform.position, targetNode.transform.position);
                    
                    // The score prioritizes closer and less defended targets. Neutral nodes get a bonus.
                    float score = 1000f / (distance * (targetNode.UnitCount + 1));
                    if (targetNode.Owner == GameManager.Instance.unclaimedFaction)
                    {
                        score *= 1.5f; // Prioritize expansion into neutral territory.
                    }

                    if (score > bestAttackScore)
                    {
                        bestAttackScore = score;
                        bestSourceNode = sourceNode;
                        bestTargetNode = targetNode;
                    }
                }
            }
        }

        if (bestSourceNode != null && bestTargetNode != null)
        {
            // Send a calculated percentage of units to be efficient.
            float unitsToSend = bestTargetNode.UnitCount + OFFENSIVE_ATTACK_MARGIN;
            float percentage = unitsToSend / bestSourceNode.UnitCount;
            bestSourceNode.SendUnits(bestTargetNode, Mathf.Clamp(percentage, 0.1f, 1.0f));
            return true; // Offensive action taken.
        }

        return false; // No suitable offensive action found.
    }

    /// <summary>
    /// **CONSOLIDATION:** Moves units from safe, populous nodes to frontline nodes.
    /// This is a fallback action to prepare for future attacks when no other moves are beneficial.
    /// </summary>
    private bool PerformConsolidationAction(List<ConstructController> myNodes)
    {
        if (myNodes.Count < 2) return false;

        // Find the node with the most units.
        var richestNode = myNodes.OrderByDescending(n => n.UnitCount).First();
        
        // Only consolidate if the richest node has a large surplus.
        if (richestNode.UnitCount < 50) return false;

        var enemies = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction).ToList();
        if (!enemies.Any()) return false;

        // Find a "forward" node, which is one of our nodes closest to an enemy.
        var forwardNode = myNodes
            .OrderBy(n => enemies.Min(e => Vector3.Distance(n.transform.position, e.transform.position)))
            .FirstOrDefault();

        // If the richest node isn't already the forward node, move units up.
        if (forwardNode != null && richestNode != forwardNode)
        {
            richestNode.SendUnits(forwardNode, 0.5f); // Send half of the units to the front.
            return true;
        }

        return false;
    }
}