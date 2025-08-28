using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This AIManager uses a dynamic scoring system to determine the best possible action each cycle.
/// It evaluates all potential moves—defensive, offensive, economic—and assigns each a score.
/// The action with the highest score is then executed. This approach makes the AI highly adaptive
/// and capable of seizing high-value opportunities, rather than following a fixed priority list.
/// </summary>
public class AIManager2 : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 0.8f;

    // A simple class to represent a potential action and its calculated score.
    private abstract class AIAction
    {
        public float Score { get; protected set; }
        public abstract void Execute();
    }

    private class AttackAction : AIAction
    {
        private readonly ConstructController _source;
        private readonly ConstructController _target;
        private readonly float _percentage;

        public AttackAction(ConstructController source, ConstructController target, float unitAdvantage, float distance)
        {
            _source = source;
            _target = target;

            // Calculate score: Higher for bigger advantages and closer targets. Neutral nodes are prioritized.
            float targetValue = (target.Owner == GameManager.Instance.unclaimedFaction) ? 1.5f : 1.0f;
            this.Score = (unitAdvantage * targetValue * 10f) / Mathf.Max(1f, distance);

            // Determine the percentage of units to send for an efficient attack.
            float unitsToSend = target.UnitCount + 5; // Send enough to win plus a small margin.
            _percentage = unitsToSend / source.UnitCount;
        }

        public override void Execute() => _source.SendUnits(_target, Mathf.Clamp(_percentage, 0.1f, 1.0f));
    }

    private class DefendAction : AIAction
    {
        private readonly ConstructController _source;
        private readonly ConstructController _target;
        private readonly int _unitsToSend;
        
        public DefendAction(ConstructController source, ConstructController target, int threatAmount)
        {
            _source = source;
            _target = target;
            _unitsToSend = threatAmount + 5; // Send enough to counter the threat plus a margin.
            
            // Defensive actions are critical and get a very high base score.
            this.Score = 1000 + (threatAmount * 10);
        }

        public override void Execute() => _source.SendExactUnits(_target, _unitsToSend);
    }

    private class UpgradeAction : AIAction
    {
        private readonly ConstructController _nodeToUpgrade;
        
        public UpgradeAction(ConstructController node)
        {
            _nodeToUpgrade = node;

            // Score is based on the upgrade's cost-effectiveness. Cheaper, more impactful upgrades are better.
            float benefit = 1f; // Base benefit for any upgrade.
            if (node.currentConstructData is HouseData current && node.currentConstructData.upgradedVersion is HouseData next)
            {
                benefit += (next.unitsPerSecond - current.unitsPerSecond) * 100; // Big bonus for economic upgrades.
            }
            this.Score = benefit * 20f / node.currentConstructData.upgradeCost;
        }
        
        public override void Execute() => _nodeToUpgrade.AttemptUpgrade();
    }
    
    private class ConsolidateAction : AIAction
    {
        private readonly ConstructController _source;
        private readonly ConstructController _target;
        
        public ConsolidateAction(ConstructController source, ConstructController target)
        {
            _source = source;
            _target = target;
            // Consolidation is a low-priority "idle" action with a small, fixed score.
            this.Score = 5f;
        }
        
        public override void Execute() => _source.SendUnits(_target, 0.5f);
    }
    
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
        yield return new WaitForSeconds(2.5f); // Wait a bit longer to observe opponent's opening moves.
        while (true)
        {
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                ExecuteBestAction();
            }
            yield return new WaitForSeconds(DECISION_DELAY);
        }
    }

    private void ExecuteBestAction()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any()) return;

        var allPossibleActions = new List<AIAction>();

        // 1. Evaluate all possible actions and add them to the list.
        allPossibleActions.Add(EvaluateDefensiveMoves(myNodes));
        allPossibleActions.Add(EvaluateOffensiveMoves(myNodes));
        allPossibleActions.Add(EvaluateUpgradeMoves(myNodes));
        allPossibleActions.Add(EvaluateConsolidationMoves(myNodes));

        // 2. Filter out nulls and find the action with the highest score.
        var bestAction = allPossibleActions
            .Where(action => action != null)
            .OrderByDescending(action => action.Score)
            .FirstOrDefault();

        // 3. Execute the best action.
        bestAction?.Execute();
    }
    
    // ## Evaluation Methods ##
    // ========================

    /// <summary>
    /// Finds the single most critical defensive action to take.
    /// </summary>
    private AIAction EvaluateDefensiveMoves(List<ConstructController> myNodes)
    {
        ConstructController mostThreatenedNode = null;
        int highestThreat = 0;

        foreach (var myNode in myNodes)
        {
            int incomingEnemyUnits = GameManager.Instance.allUnits
                .Count(unit => unit.owner != aiFaction && unit.target == myNode);
                
            int threatLevel = incomingEnemyUnits - myNode.UnitCount;

            if (threatLevel > highestThreat)
            {
                highestThreat = threatLevel;
                mostThreatenedNode = myNode;
            }
        }
        
        if (mostThreatenedNode != null)
        {
            var bestReinforcer = myNodes
                .Where(n => n != mostThreatenedNode && n.UnitCount > highestThreat + 5)
                .OrderBy(n => Vector3.Distance(n.transform.position, mostThreatenedNode.transform.position))
                .FirstOrDefault();

            if (bestReinforcer != null)
            {
                return new DefendAction(bestReinforcer, mostThreatenedNode, highestThreat);
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the single best offensive or expansion move.
    /// </summary>
    private AIAction EvaluateOffensiveMoves(List<ConstructController> myNodes)
    {
        var targets = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction).ToList();
        if (!targets.Any()) return null;

        AIAction bestAttack = null;

        foreach (var sourceNode in myNodes.Where(n => n.UnitCount > 10))
        {
            foreach (var targetNode in targets)
            {
                int unitAdvantage = sourceNode.UnitCount - targetNode.UnitCount;
                if (unitAdvantage > 5) // Must have a clear advantage to even consider an attack.
                {
                    var potentialAction = new AttackAction(sourceNode, targetNode, unitAdvantage,
                        Vector3.Distance(sourceNode.transform.position, targetNode.transform.position));
                    
                    if (bestAttack == null || potentialAction.Score > bestAttack.Score)
                    {
                        bestAttack = potentialAction;
                    }
                }
            }
        }
        return bestAttack;
    }
    
    /// <summary>
    /// Finds the single most valuable upgrade to perform.
    /// </summary>
    private AIAction EvaluateUpgradeMoves(List<ConstructController> myNodes)
    {
        // To avoid starving itself of units, AI won't upgrade if its total unit count is low.
        if (myNodes.Sum(n => n.UnitCount) < 80) return null;
        
        AIAction bestUpgrade = null;

        var upgradeableNodes = myNodes.Where(n => n.currentConstructData.upgradedVersion != null &&
                                                 n.UnitCount >= n.currentConstructData.upgradeCost);
                                                 
        foreach(var node in upgradeableNodes)
        {
            var potentialAction = new UpgradeAction(node);
            if(bestUpgrade == null || potentialAction.Score > bestUpgrade.Score)
            {
                bestUpgrade = potentialAction;
            }
        }
        return bestUpgrade;
    }
    
    /// <summary>
    /// Finds a logical consolidation move if no better options are available.
    /// </summary>
    private AIAction EvaluateConsolidationMoves(List<ConstructController> myNodes)
    {
        if (myNodes.Count < 2) return null;

        var richestNode = myNodes.OrderByDescending(n => n.UnitCount).First();
        if (richestNode.UnitCount < 40) return null; // Only consolidate from a strong position.

        var enemies = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction).ToList();
        if (!enemies.Any()) return null;

        var forwardNode = myNodes
            .OrderBy(n => enemies.Min(e => Vector3.Distance(n.transform.position, e.transform.position)))
            .FirstOrDefault();

        if (forwardNode != null && richestNode != forwardNode)
        {
            return new ConsolidateAction(richestNode, forwardNode);
        }
        return null;
    }
}