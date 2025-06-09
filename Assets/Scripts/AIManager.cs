using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AIManager : MonoBehaviour
{
    public FactionData aiFaction;
    public AIDifficultySettings difficulty;
    private float timeSinceLastAction = 0f;

    void Start()
    {
        if (difficulty == null)
        {
            this.enabled = false;
            return;
        }
        StartCoroutine(MakeDecisionsRoutine());
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing) return;
        
        timeSinceLastAction += Time.deltaTime;

        if (timeSinceLastAction > 5f)
        {
            ForceIdleAction();
        }
    }

    private IEnumerator MakeDecisionsRoutine()
    {
        while (GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            yield return new WaitForSeconds(difficulty.decisionCooldown);
            ExecuteBestAction();
        }
    }

    private void ExecuteBestAction()
    {
        List<ConstructController> myNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner == aiFaction).ToList();
        
        if (!myNodes.Any()) return;

        float upgradeChance = 0.2f;
        if (Random.value < upgradeChance)
        {
            if (PerformUpgradeAction(myNodes))
            {
                return;
            }
        }

        List<ConstructController> vulnerableNodes = myNodes
            .Where(n => n.UnitCount < difficulty.reinforceThreshold).ToList();

        if (vulnerableNodes.Any() && Random.value > difficulty.aggressionChance)
        {
            if (PerformDefensiveAction(myNodes, vulnerableNodes))
            {
                return;
            }
        }
        
        PerformOffensiveAction(myNodes);
    }
    
    private bool PerformUpgradeAction(List<ConstructController> myNodes)
    {
        var upgradeableNodes = myNodes
            .Where(n => n.visuals.GetComponent<ConstructController>().currentConstructData.upgradedVersion != null)
            .OrderByDescending(n => n.UnitCount)
            .ToList();

        if (!upgradeableNodes.Any()) return false;

        ConstructController candidate = upgradeableNodes.First();
        float upgradeCost = candidate.visuals.GetComponent<ConstructController>().currentConstructData.upgradeCost;

        if (candidate.UnitCount >= upgradeCost)
        {
            candidate.AttemptUpgrade();
            timeSinceLastAction = 0f;
            return true;
        }
        else
        {
            ConstructController bestReinforcer = myNodes
                .Where(n => n != candidate && n.UnitCount > difficulty.reinforceThreshold)
                .OrderByDescending(n => n.UnitCount)
                .FirstOrDefault();
            
            if (bestReinforcer != null)
            {
                bestReinforcer.SendUnits(candidate, 0.5f);
                timeSinceLastAction = 0f;
                return true;
            }
        }
        return false;
    }

    private bool PerformOffensiveAction(List<ConstructController> myNodes)
    {
        List<ConstructController> attackableNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction).ToList();

        if (!myNodes.Any() || !attackableNodes.Any()) return false;

        float bestScore = float.MinValue;
        ConstructController bestSourceNode = null;
        ConstructController bestTargetNode = null;

        foreach (var sourceNode in myNodes)
        {
            if (sourceNode.UnitCount < difficulty.attackAdvantageMargin) continue;

            foreach (var targetNode in attackableNodes)
            {
                if (sourceNode.UnitCount > targetNode.UnitCount + difficulty.attackAdvantageMargin)
                {
                    float distance = Vector3.Distance(sourceNode.transform.position, targetNode.transform.position);
                    float score = (sourceNode.UnitCount - targetNode.UnitCount) - (distance * difficulty.proximityWeight);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSourceNode = sourceNode;
                        bestTargetNode = targetNode;
                    }
                }
            }
        }

        if (bestSourceNode != null && bestTargetNode != null)
        {
            bestSourceNode.SendUnits(bestTargetNode, 0.75f);
            timeSinceLastAction = 0f;
            return true;
        }
        return false;
    }

    private bool PerformDefensiveAction(List<ConstructController> myNodes, List<ConstructController> vulnerableNodes)
    {
        if (myNodes.Count <= 1 || !vulnerableNodes.Any()) return false;

        ConstructController targetNode = vulnerableNodes.OrderBy(n => n.UnitCount).FirstOrDefault();
        if (targetNode == null) return false;
        
        ConstructController bestSourceNode = myNodes
            .Where(n => n != targetNode && n.UnitCount > targetNode.UnitCount)
            .OrderByDescending(n => n.UnitCount)
            .FirstOrDefault();

        if (bestSourceNode != null)
        {
            bestSourceNode.SendUnits(targetNode, 0.5f);
            timeSinceLastAction = 0f;
            return true;
        }
        return false;
    }

    private void ForceIdleAction()
    {
        timeSinceLastAction = 0f;
        
        List<ConstructController> myNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner == aiFaction).ToList();

        if (!myNodes.Any()) return;

        List<ConstructController> vulnerableNodes = myNodes
            .Where(n => n.UnitCount < difficulty.reinforceThreshold).ToList();

        if (vulnerableNodes.Any())
        {
            if(PerformDefensiveAction(myNodes, vulnerableNodes)) return;
        }
        
        PerformOffensiveAction(myNodes);
    }
}