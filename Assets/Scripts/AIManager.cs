using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AIManager : MonoBehaviour
{
    public FactionData aiFaction;
    public AIDifficultySettings difficulty;

    void Start()
    {
        if (difficulty == null)
        {
            Debug.LogError("AIManager requires an AIDifficultySettings asset to be assigned.", this);
            this.enabled = false;
            return;
        }
        StartCoroutine(MakeDecisionsRoutine());
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
        
        List<ConstructController> vulnerableNodes = myNodes
            .Where(n => n.UnitCount < difficulty.reinforceThreshold).ToList();

        if (Random.value > difficulty.aggressionChance && vulnerableNodes.Any())
        {
            PerformDefensiveAction(myNodes, vulnerableNodes);
        }
        else
        {
            PerformOffensiveAction(myNodes);
        }
    }

    private void PerformOffensiveAction(List<ConstructController> myNodes)
    {
        List<ConstructController> attackableNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction).ToList();

        if (!myNodes.Any() || !attackableNodes.Any()) return;

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
        }
    }

    private void PerformDefensiveAction(List<ConstructController> myNodes, List<ConstructController> vulnerableNodes)
    {
        if (myNodes.Count <= 1) return;

        ConstructController targetNode = vulnerableNodes.OrderBy(n => n.UnitCount).FirstOrDefault();
        if (targetNode == null) return;
        
        ConstructController bestSourceNode = myNodes
            .Where(n => n != targetNode && n.UnitCount > targetNode.UnitCount)
            .OrderByDescending(n => n.UnitCount)
            .FirstOrDefault();

        if (bestSourceNode != null)
        {
            bestSourceNode.SendUnits(targetNode, 0.5f);
        }
    }
}
