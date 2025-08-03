using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AIManager5 is a holistic, ROI-driven AI. It aims to be the best possible generalist
/// by making quantitatively sound decisions. It evaluates every possible move (attacking,
/// defending, upgrading, expanding) based on its long-term value and executes the one
/// with the highest score. It prioritizes building a strong economy and expanding early,
/// transitioning to military dominance once its production is superior.
/// </summary>
public class AIManager5 : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 0.5f; // Thinks very quickly to adapt to the game state.

    // A simple class to hold an action and its calculated score.
    private abstract class AIAction
    {
        public float Score { get; protected set; }
        public abstract string Description { get; }
        public abstract void Execute();
    }
    
    // Concrete action classes
    private class DefendAction : AIAction 
    {
        private ConstructController source, target;
        private int units;
        public override string Description => $"Defense: Sending {units} from {source.name} to {target.name}";
        public DefendAction(ConstructController src, ConstructController tgt, int threat)
        {
            source = src;
            target = tgt;
            units = threat + 5; // Send enough to win plus a buffer.
            // Defense is critical. The score scales massively with the size of the threat.
            Score = 1000 + (threat * 20);
        }
        public override void Execute() => source.SendExactUnits(target, units);
    }
    private class ExpandAction : AIAction 
    {
        private ConstructController source, target;
        private float percentage;
        public override string Description => $"Expansion: Sending {Mathf.Round(percentage*100)}% from {source.name} to capture {target.name}";
        public ExpandAction(ConstructController src, ConstructController tgt)
        {
            source = src;
            target = tgt;
            int unitsToSend = tgt.UnitCount + 5;
            percentage = (float)unitsToSend / src.UnitCount;

            // Score is based on how cheap and close the target is. Early game expansion is heavily prioritized.
            float earlyGameBonus = Mathf.Clamp(3.0f - (Time.timeSinceLevelLoad / 60f), 1.0f, 3.0f);
            float distance = Vector3.Distance(src.transform.position, tgt.transform.position);
            Score = (2000f / (distance * (tgt.UnitCount + 1))) * earlyGameBonus;
        }
        public override void Execute() => source.SendUnits(target, percentage);
    }
    private class AttackAction : AIAction 
    {
        private ConstructController source, target;
        private float percentage;
        public override string Description => $"Attack: Sending {Mathf.Round(percentage*100)}% from {source.name} to attack {target.name}";
        public AttackAction(ConstructController src, ConstructController tgt)
        {
            source = src;
            target = tgt;
            int unitsToSend = tgt.UnitCount + 10; // Need a larger advantage when attacking a live player.
            percentage = (float)unitsToSend / src.UnitCount;

            // Score is based on the value of defeating the enemy vs. the cost.
            float targetValue = 50 + target.UnitCount;
            float distance = Vector3.Distance(src.transform.position, tgt.transform.position);
            Score = targetValue / distance;
        }
        public override void Execute() => source.SendUnits(target, percentage);
    }
    private class UpgradeAction : AIAction 
    {
        private ConstructController node;
        public override string Description => $"Upgrade: Upgrading {node.name}";
        public UpgradeAction(ConstructController n)
        {
            node = n;
            // Calculate the Return on Investment (ROI) for the upgrade.
            float benefit = 10f; // Base value for any upgrade.
            if(node.currentConstructData is HouseData current && node.currentConstructData.upgradedVersion is HouseData next)
            {
                // The benefit of a house upgrade is its increased unit production over the lifetime of the game.
                benefit = (next.unitsPerSecond - current.unitsPerSecond) * 500f;
            }
            Score = benefit / node.currentConstructData.upgradeCost;
        }
        public override void Execute() => node.AttemptUpgrade();
    }
    private class ConsolidateAction : AIAction 
    {
        private ConstructController source, target;
        public override string Description => $"Consolidate: Moving units from {source.name} to {target.name}";
        public ConsolidateAction(ConstructController src, ConstructController tgt)
        {
            source = src;
            target = tgt;
            // A low, fixed score makes this a default "idle" action.
            Score = 5f;
        }
        public override void Execute() => source.SendUnits(target, 0.5f);
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
        yield return new WaitForSeconds(2.0f);
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

        var neutralNodes = GameManager.Instance.allConstructs.Where(n => n.Owner == GameManager.Instance.unclaimedFaction).ToList();
        var enemyNodes = GameManager.Instance.allConstructs.Where(n => n.Owner != aiFaction && n.Owner != GameManager.Instance.unclaimedFaction).ToList();

        // Evaluate the best action in each category.
        allPossibleActions.Add(FindBestDefense(myNodes));
        allPossibleActions.Add(FindBestExpansion(myNodes, neutralNodes));
        allPossibleActions.Add(FindBestAttack(myNodes, enemyNodes));
        allPossibleActions.Add(FindBestUpgrade(myNodes));
        allPossibleActions.Add(FindBestConsolidation(myNodes, enemyNodes));

        // Find the action with the highest score and execute it.
        var bestAction = allPossibleActions
            .Where(action => action != null)
            .OrderByDescending(action => action.Score)
            .FirstOrDefault();

        bestAction?.Execute();
    }
    
    // ## Evaluation Functions ##
    
    private AIAction FindBestDefense(List<ConstructController> myNodes)
    {
        AIAction bestDefense = null;
        foreach (var myNode in myNodes)
        {
            int threat = GameManager.Instance.allUnits.Count(u => u.owner != aiFaction && u.target == myNode);
            if (threat > myNode.UnitCount)
            {
                var reinforcer = myNodes
                    .Where(n => n != myNode && n.UnitCount > threat - myNode.UnitCount + 5)
                    .OrderBy(n => Vector3.Distance(n.transform.position, myNode.transform.position))
                    .FirstOrDefault();
                
                if (reinforcer != null)
                {
                    var defenseAction = new DefendAction(reinforcer, myNode, threat - myNode.UnitCount);
                    if (bestDefense == null || defenseAction.Score > bestDefense.Score)
                    {
                        bestDefense = defenseAction;
                    }
                }
            }
        }
        return bestDefense;
    }

    private AIAction FindBestExpansion(List<ConstructController> myNodes, List<ConstructController> neutralNodes)
    {
        AIAction bestExpansion = null;
        if (!neutralNodes.Any()) return null;

        foreach (var source in myNodes.Where(n => n.UnitCount > 10))
        {
            foreach (var target in neutralNodes)
            {
                if (source.UnitCount > target.UnitCount + 5)
                {
                    var expansionAction = new ExpandAction(source, target);
                    if (bestExpansion == null || expansionAction.Score > bestExpansion.Score)
                    {
                        bestExpansion = expansionAction;
                    }
                }
            }
        }
        return bestExpansion;
    }

    private AIAction FindBestAttack(List<ConstructController> myNodes, List<ConstructController> enemyNodes)
    {
        AIAction bestAttack = null;
        if (!enemyNodes.Any()) return null;

        // Be more cautious about attacking live players.
        if (myNodes.Sum(n => n.UnitCount) < 150) return null;

        foreach (var source in myNodes.Where(n => n.UnitCount > 30))
        {
            foreach (var target in enemyNodes)
            {
                if (source.UnitCount > target.UnitCount + 10)
                {
                    var attackAction = new AttackAction(source, target);
                    if (bestAttack == null || attackAction.Score > bestAttack.Score)
                    {
                        bestAttack = attackAction;
                    }
                }
            }
        }
        return bestAttack;
    }

    private AIAction FindBestUpgrade(List<ConstructController> myNodes)
    {
        AIAction bestUpgrade = null;
        var safeNodes = myNodes.Where(n => !GameManager.Instance.allUnits.Any(u => u.target == n && u.owner != aiFaction));

        foreach(var node in safeNodes)
        {
            if (node.currentConstructData.upgradedVersion != null && node.UnitCount >= node.currentConstructData.upgradeCost)
            {
                var upgradeAction = new UpgradeAction(node);
                if(bestUpgrade == null || upgradeAction.Score > bestUpgrade.Score)
                {
                    bestUpgrade = upgradeAction;
                }
            }
        }
        return bestUpgrade;
    }

    private AIAction FindBestConsolidation(List<ConstructController> myNodes, List<ConstructController> enemyNodes)
    {
        if (myNodes.Count < 2 || !enemyNodes.Any()) return null;

        var richestNode = myNodes.OrderByDescending(n => n.UnitCount).FirstOrDefault();
        if (richestNode == null || richestNode.UnitCount < 50) return null;
        
        var forwardNode = myNodes.OrderBy(n => Vector3.Distance(n.transform.position, enemyNodes.First().transform.position)).FirstOrDefault();
        
        if (forwardNode != null && richestNode != forwardNode)
        {
            return new ConsolidateAction(richestNode, forwardNode);
        }
        return null;
    }

} // <-- Make sure ALL of the code above is inside this final closing brace.