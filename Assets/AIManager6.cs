using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This AIManager uses a "Momentum" strategy, dynamically changing its playstyle
/// based on whether it perceives itself as winning, losing, or in an even match.
/// This allows it to press advantages aggressively and defend intelligently when behind.
/// </summary>
public class AIManager6 : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 0.75f;

    private enum GameMomentum { Winning, Losing, Even }
    private GameMomentum currentMomentum;

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
        yield return new WaitForSeconds(2.5f);
        while (true)
        {
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                // The core of the AI: assess the situation, then act accordingly.
                AssessMomentum();
                ExecuteStrategy();
            }
            yield return new WaitForSeconds(DECISION_DELAY);
        }
    }

    /// <summary>
    /// Calculates the AI's power relative to its strongest opponent to determine the game's momentum.
    /// </summary>
    private void AssessMomentum()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any())
        {
            currentMomentum = GameMomentum.Losing;
            return;
        }

        // Find the strongest opponent to compare against.
        var strongestOpponent = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction)
            .GroupBy(c => c.Owner)
            .OrderByDescending(g => g.Sum(n => n.UnitCount))
            .FirstOrDefault()?.Key;

        if (strongestOpponent == null)
        {
            currentMomentum = GameMomentum.Winning; // No opponents left.
            return;
        }
        
        var opponentNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == strongestOpponent).ToList();

        // Calculate power scores based on unit count and production rate.
        float myPower = myNodes.Sum(n => n.UnitCount) + (myNodes.Sum(n => n.currentConstructData.unitsPerSecond) * 20);
        float opponentPower = opponentNodes.Sum(n => n.UnitCount) + (opponentNodes.Sum(n => n.currentConstructData.unitsPerSecond) * 20);

        if (myPower > opponentPower * 1.4f)
        {
            currentMomentum = GameMomentum.Winning;
        }
        else if (opponentPower > myPower * 1.4f)
        {
            currentMomentum = GameMomentum.Losing;
        }
        else
        {
            currentMomentum = GameMomentum.Even;
        }
    }

    /// <summary>
    /// Calls the appropriate strategy handler based on the current momentum.
    /// </summary>
    private void ExecuteStrategy()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any()) return;

        // Always perform critical defense, regardless of momentum.
        if (PerformCriticalDefense(myNodes)) return;

        switch (currentMomentum)
        {
            case GameMomentum.Winning:
                ExecuteAggressiveStrategy(myNodes);
                break;
            case GameMomentum.Losing:
                ExecuteDefensiveStrategy(myNodes);
                break;
            case GameMomentum.Even:
                ExecuteBalancedStrategy(myNodes);
                break;
        }
    }

    // ## Core Strategy Handlers ##

    private void ExecuteAggressiveStrategy(List<ConstructController> myNodes)
    {
        // When winning, the priority is to attack relentlessly.
        if (PerformAggressiveAttack(myNodes)) return;
        if (PerformExpansion(myNodes)) return; // Capture neutrals if no good attacks exist.
        PerformUpgrade(myNodes); // Upgrade as a last resort.
    }

    private void ExecuteDefensiveStrategy(List<ConstructController> myNodes)
    {
        // When losing, the priority is to survive and rebuild.
        if (PerformConsolidation(myNodes)) return; // Pull forces back.
        if (PerformUpgrade(myNodes)) return; // Focus on economy.
        PerformExpansion(myNodes); // Only expand to very easy targets.
    }
    
    private void ExecuteBalancedStrategy(List<ConstructController> myNodes)
    {
        // When even, prioritize smart growth.
        if (PerformExpansion(myNodes)) return;
        if (PerformUpgrade(myNodes)) return;
        if (PerformAggressiveAttack(myNodes)) return; // Attack only with a clear advantage.
        PerformConsolidation(myNodes);
    }

    // ## Action Implementations ##

    private bool PerformCriticalDefense(List<ConstructController> myNodes)
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
            var reinforcer = myNodes
                .Where(n => n != mostThreatenedNode && n.UnitCount > highestThreat + 5)
                .OrderBy(n => Vector3.Distance(n.transform.position, mostThreatenedNode.transform.position))
                .FirstOrDefault();
            if (reinforcer != null)
            {
                reinforcer.SendExactUnits(mostThreatenedNode, highestThreat + 5);
                return true;
            }
        }
        return false;
    }

    private bool PerformAggressiveAttack(List<ConstructController> myNodes)
    {
        var enemyNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction).ToList();
        if (!enemyNodes.Any()) return false;

        // In "Winning" mode, the required advantage is smaller to maintain pressure.
        int requiredAdvantage = (currentMomentum == GameMomentum.Winning) ? 5 : 15;
        
        var possibleAttacks = new List<(ConstructController source, ConstructController target)>();
        foreach (var source in myNodes.Where(n => n.UnitCount > requiredAdvantage + 10))
        {
            foreach (var target in enemyNodes)
            {
                if (source.UnitCount > target.UnitCount + requiredAdvantage)
                {
                    possibleAttacks.Add((source, target));
                }
            }
        }

        if (possibleAttacks.Any())
        {
            // Attack the enemy node that is closest to one of our attackers.
            var bestAttack = possibleAttacks
                .OrderBy(t => Vector3.Distance(t.source.transform.position, t.target.transform.position))
                .First();
            
            bestAttack.source.SendUnits(bestAttack.target, 0.75f);
            return true;
        }
        return false;
    }

    private bool PerformExpansion(List<ConstructController> myNodes)
    {
        var neutralNodes = GameManager.Instance.allConstructs.Where(n => n.Owner == GameManager.Instance.unclaimedFaction).ToList();
        if (!neutralNodes.Any()) return false;
        
        var possibleExpansions = new List<(ConstructController source, ConstructController target)>();
        foreach (var source in myNodes.Where(n => n.UnitCount > 10))
        {
            foreach (var target in neutralNodes)
            {
                if (source.UnitCount > target.UnitCount + 3)
                {
                    possibleExpansions.Add((source, target));
                }
            }
        }

        if (possibleExpansions.Any())
        {
            // Find the easiest, closest neutral node to capture.
            var bestExpansion = possibleExpansions
                .OrderBy(t => t.target.UnitCount)
                .ThenBy(t => Vector3.Distance(t.source.transform.position, t.target.transform.position))
                .First();
            
            bestExpansion.source.SendUnits(bestExpansion.target, 0.5f);
            return true;
        }
        return false;
    }
    
    private bool PerformUpgrade(List<ConstructController> myNodes)
    {
        var nodeToUpgrade = myNodes
            .Where(n => n.currentConstructData.upgradedVersion != null && n.UnitCount >= n.currentConstructData.upgradeCost)
            .OrderByDescending(n => n.currentConstructData is HouseData) // Prioritize economy
            .ThenBy(n => n.currentConstructData.upgradeCost) // Then cheapest
            .FirstOrDefault();

        if (nodeToUpgrade != null)
        {
            nodeToUpgrade.AttemptUpgrade();
            return true;
        }
        return false;
    }
    
    private bool PerformConsolidation(List<ConstructController> myNodes)
    {
        if (myNodes.Count < 2) return false;
        
        var richestNode = myNodes.OrderByDescending(n => n.UnitCount).FirstOrDefault();
        if (richestNode == null || richestNode.UnitCount < 30) return false;

        // Find the weakest friendly node to reinforce.
        var weakestNode = myNodes.OrderBy(n => n.UnitCount).FirstOrDefault();

        if (weakestNode != null && richestNode != weakestNode)
        {
            richestNode.SendUnits(weakestNode, 0.5f);
            return true;
        }
        return false;
    }
}