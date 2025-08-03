using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This AIManager operates as a "Warlord," using a sophisticated understanding of map control and
/// positional advantage. It classifies its constructs into roles (Frontline, Economic, Support)
/// based on their proximity to danger and strategic value, then executes a cohesive, territory-based strategy.
/// </summary>
public class AIManager7 : MonoBehaviour
{
    public FactionData aiFaction;
    private const float DECISION_DELAY = 0.6f;

    // Defines the tactical role of a construct.
    private enum NodeRole { Frontline, Support, Economic }

    // A dictionary to store the dynamically assigned role of each construct.
    private Dictionary<ConstructController, NodeRole> nodeRoles = new Dictionary<ConstructController, NodeRole>();

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
                ExecuteWarlordStrategy();
            }
            yield return new WaitForSeconds(DECISION_DELAY);
        }
    }

    /// <summary>
    /// The core loop: First, analyze the map to assign roles, then execute actions based on those roles.
    /// </summary>
    private void ExecuteWarlordStrategy()
    {
        var myNodes = GameManager.Instance.allConstructs.Where(c => c.Owner == aiFaction).ToList();
        if (!myNodes.Any()) return;
        
        AnalyzeMapAndAssignRoles(myNodes);
        
        // Actions are decided based on priority, with role-specific logic.
        if (PerformDefensiveActions(myNodes)) return;
        if (PerformOffensiveActions(myNodes)) return;
        if (PerformEconomicActions(myNodes)) return;
        PerformLogisticalActions(myNodes);
    }

    /// <summary>
    /// **CORRECTED:** Analyzes the battlefield and assigns roles using a relative, percentage-based system.
    /// This is now adaptable to any map size and will not stall.
    /// </summary>
    private void AnalyzeMapAndAssignRoles(List<ConstructController> myNodes)
    {
        nodeRoles.Clear();
        var enemyNodes = GameManager.Instance.allConstructs
            .Where(c => c.Owner != aiFaction && c.Owner != GameManager.Instance.unclaimedFaction)
            .ToList();

        // If no enemies exist or we have too few nodes for complex roles, they are all economic.
        if (!enemyNodes.Any() || myNodes.Count <= 2)
        {
            myNodes.ForEach(n => nodeRoles[n] = NodeRole.Economic);
            return;
        }

        Vector3 enemyCenter = GetFactionCenter(enemyNodes);
        
        // Sort nodes by their distance to the enemy's center of mass.
        var sortedNodes = myNodes.OrderBy(n => Vector3.Distance(n.transform.position, enemyCenter)).ToList();

        int nodeCount = sortedNodes.Count;
        // Designate roles by percentage: closest 30% are Frontline, next 40% are Support, rest are Economic.
        int frontlineCount = Mathf.Max(1, Mathf.CeilToInt(nodeCount * 0.3f)); // Ensure at least 1 frontline node.
        int supportCount = Mathf.CeilToInt(nodeCount * 0.4f);

        for (int i = 0; i < nodeCount; i++)
        {
            ConstructController currentNode = sortedNodes[i];
            if (i < frontlineCount)
            {
                nodeRoles[currentNode] = NodeRole.Frontline;
            }
            else if (i < frontlineCount + supportCount)
            {
                nodeRoles[currentNode] = NodeRole.Support;
            }
            else
            {
                nodeRoles[currentNode] = NodeRole.Economic;
            }
        }
    }
    
    // ## Role-Based Action Implementations ##

    private bool PerformDefensiveActions(List<ConstructController> myNodes)
    {
        var threatenedNodeInfo = myNodes
            .Select(n => new { Node = n, Threat = GameManager.Instance.allUnits.Count(u => u.target == n && u.owner != aiFaction) })
            .Where(x => x.Threat > x.Node.UnitCount)
            .OrderByDescending(x => nodeRoles.ContainsKey(x.Node) && nodeRoles[x.Node] == NodeRole.Frontline) // Prioritize frontline defense
            .ThenByDescending(x => x.Threat)
            .FirstOrDefault();

        if (threatenedNodeInfo != null)
        {
            var reinforcer = myNodes
                .Where(n => n != threatenedNodeInfo.Node && n.UnitCount > threatenedNodeInfo.Threat + 5)
                .OrderBy(n => nodeRoles.ContainsKey(n) && nodeRoles[n] == NodeRole.Support) // Prefer reinforcements from Support nodes
                .ThenBy(n => Vector3.Distance(n.transform.position, threatenedNodeInfo.Node.transform.position))
                .FirstOrDefault();

            if (reinforcer != null)
            {
                reinforcer.SendExactUnits(threatenedNodeInfo.Node, threatenedNodeInfo.Threat - threatenedNodeInfo.Node.UnitCount + 5);
                return true;
            }
        }
        return false;
    }

    private bool PerformOffensiveActions(List<ConstructController> myNodes)
    {
        if (!nodeRoles.Any(kvp => kvp.Value == NodeRole.Frontline)) return false;

        var frontlineAttackers = myNodes.Where(n => nodeRoles[n] == NodeRole.Frontline && n.UnitCount > 25);
        if (!frontlineAttackers.Any()) return false;

        var allTargets = GameManager.Instance.allConstructs.Where(c => c.Owner != aiFaction).ToList();
        if (!allTargets.Any()) return false;

        var bestAttack = frontlineAttackers
            .SelectMany(source => allTargets.Select(target => new { source, target }))
            .Where(t => t.source.UnitCount > t.target.UnitCount + 10)
            .OrderBy(t => t.target.Owner == GameManager.Instance.unclaimedFaction ? 0 : 1) // Prioritize neutrals
            .ThenBy(t => Vector3.Distance(t.source.transform.position, t.target.transform.position))
            .FirstOrDefault();

        if (bestAttack != null)
        {
            bestAttack.source.SendUnits(bestAttack.target, 0.75f);
            return true;
        }
        return false;
    }

    private bool PerformEconomicActions(List<ConstructController> myNodes)
    {
        if (!nodeRoles.Any(kvp => kvp.Value == NodeRole.Economic)) return false;

        var nodeToUpgrade = myNodes
            .Where(n => nodeRoles[n] == NodeRole.Economic &&
                        n.currentConstructData.upgradedVersion != null &&
                        n.UnitCount >= n.currentConstructData.upgradeCost)
            .OrderByDescending(n => n.currentConstructData is HouseData)
            .FirstOrDefault();

        if (nodeToUpgrade != null)
        {
            nodeToUpgrade.AttemptUpgrade();
            return true;
        }
        return false;
    }

    private bool PerformLogisticalActions(List<ConstructController> myNodes)
    {
        var economicNodes = myNodes.Where(n => nodeRoles.ContainsKey(n) && nodeRoles[n] == NodeRole.Economic && n.UnitCount > 40).ToList();
        if (!economicNodes.Any()) return false;

        var frontlineNodes = myNodes.Where(n => nodeRoles.ContainsKey(n) && nodeRoles[n] == NodeRole.Frontline).ToList();
        if (!frontlineNodes.Any()) return false;

        var supplySource = economicNodes.OrderByDescending(n => n.UnitCount).First();
        var supplyTarget = frontlineNodes.OrderBy(n => n.UnitCount).First();
        
        supplySource.SendUnits(supplyTarget, 0.5f);
        return true;
    }
    
    // ## Helper Functions ##

    private Vector3 GetFactionCenter(List<ConstructController> nodes)
    {
        if (nodes == null || !nodes.Any()) return Vector3.zero;
        Vector3 center = Vector3.zero;
        nodes.ForEach(n => center += n.transform.position);
        return center / nodes.Count;
    }
}