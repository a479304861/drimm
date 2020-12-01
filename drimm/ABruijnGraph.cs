using System;
using System.Collections.Generic;
using Util.Collection;

namespace SyntenyFast
{
    public class ABruijnGraph : IABruijnGraph
    {
        private readonly IGraphTool _graphTool;
        private IList<int> _sequence;
        private SimpleLinkList<int> _workingSequence = new SimpleLinkList<int>();
        private SimpleLinkList<int> _sourceSequence = new SimpleLinkList<int>();
        IDictionary<int, IList<Node<int>>> _graph;
        IDictionary<Node<int>, Node<int>> _workToSource= new Dictionary<Node<int>, Node<int>>();
        IDictionary<Node<int>, Pair<int>> _nodeToIndex = new Dictionary<Node<int>, Pair<int>>();

        public ABruijnGraph(IGraphTool graphTool)
        {
            _graphTool = graphTool;
            
        }

        /// <summary>
        /// Thread a sequence of integer through an ABruijn Graph
        /// </summary>
        /// <param name="sequence">sequence wanted to thread</param>
        public void ThreadSequence(IList<int> sequence)
        {
            _sequence = sequence;
            _workingSequence.AddList(sequence);         //把List转换成单链表
            _sourceSequence.AddList(sequence);         //把List转换成单链表
            IList<Node<int>> _workingMembers= _workingSequence.GetMembers();
            IList<Node<int>> _sourceMembers= _sourceSequence.GetMembers();
        
            IList<IList<Node<int>>> sourceSequencesChrs = new List<IList<Node<int>>>();       //获得原序列对应的坐标
            IList<Node<int>> chr = new List<Node<int>>();
            for (int i = 1; i < _sourceMembers.Count - 1; i++)
            {
                if (_sourceMembers[i].Value >= 0)
                    chr.Add(_sourceMembers[i]);
                else
                {
                    sourceSequencesChrs.Add(chr);
                    chr = new List<Node<int>>();
                    while (i < _sourceMembers.Count && _sourceMembers[i].Value < 0)
                    {
                        i++;
                    }
                    i--;
                }
            }
            sourceSequencesChrs.Add(chr);         //拆分成Chrs

            for(int i = 0; i < sourceSequencesChrs.Count; i++)
            {
                for(int j = 0; j < sourceSequencesChrs[i].Count; j++)
                {
                    _nodeToIndex.Add(sourceSequencesChrs[i][j], new Pair<int>(i,j));
                }
            }
            for (int i = 0; i < _workingMembers.Count; i++)         //生成map
            {
                 _workToSource.Add(_workingMembers[i], _sourceMembers[i]);
            }   
            _graph = GenerateGraph(_workingSequence);
            _graphTool.setWorkToSource(_workToSource);
        }


        /// <summary>
        /// Return the list nodes in the modified sequence
        /// </summary>
        /// <returns></returns>
        public IList<int> GetModifiedSequence()
        {
            return _workingSequence.GetMembersValue();
        }
        public IList<Node<int>> GetModifiedNodeSequence()
        {
            return _workingSequence.GetMembers();
        }
        public IList<Node<int>> GetSourceSequence()
        {
            return _sourceSequence.GetMembers();
        }
        public IDictionary<Node<int>, Node<int>> GetWorkToSource()
        {
            return _graphTool.getWorkToSource();
        }
        public IDictionary<Node<int>, Pair<int>> GetNodeToIndex()
        {
            return _nodeToIndex;
        }


        /// <summary>
        /// Simplify the graph by removing the cycles and smoothing techniques. On the way of removing the cycle, if any nodes 
        /// disappear from the graph, the edge color mapping should be return back
        /// </summary>
        /// <param name="cycleLenghtThreshold">The maximum cycle length that can be re-route</param>
        /// <param name="smoothingThreshold">the maximum length of the simple path that can be splitted</param>
        /// <param name="shouldSmooth">If we should smooth the graph</param>
        /// <param name="splitNodes">Split nodes Set</param>
        /// <returns>Color edge Set</returns>
        public HashSet<KeyValuePair<int, int>> Simplify(int cycleLenghtThreshold, int smoothingThreshold, bool shouldSmooth,out HashSet<int> splitNodes)
        {
            splitNodes = new HashSet<int>();
            HashSet<KeyValuePair<int, int>> colorEdges = new HashSet<KeyValuePair<int, int>>();
            IDictionary<Pair<int>,int> multiplicityByEdge =  GenerateMultiplicityByEdge(_workingSequence.GetMembersValue());        //用字典记录有多少边,key是边，value是个数
            Console.Out.WriteLine("Number of edges: " + multiplicityByEdge.Count);
            IList<Pair<int>> weakEdges = _graphTool.GetWeakEdges(multiplicityByEdge);       //找到循环
            while (weakEdges.Count != 0)
            {
                Pair<int> currentWeakEdge = weakEdges[0];
                weakEdges.RemoveAt(0);
                HashSet<KeyValuePair<int, int>> suspectedWeakEdges = _graphTool.ReSolveCycle(currentWeakEdge, cycleLenghtThreshold, ref _graph);        //去除循环，返回颜色边界
                if (suspectedWeakEdges != null)
                    foreach (KeyValuePair<int, int> edge in suspectedWeakEdges)         //标记所有颜色边界
                        if (!colorEdges.Contains(edge))
                            colorEdges.Add(edge);
                /*
                foreach (pair<int> edge in suspectedweakedges)
                    weakedges.insert(0, edge);
                 */
            }
            Console.Out.WriteLine("Nodes:" + _graph.Keys.Count);
            //平滑需要
            if (shouldSmooth){
                //PRocess tandem Repeat A-A
                _graphTool.ProcessTandem(ref _workingSequence, ref _graph);
                //smoothing step
                IDictionary<Pair<int>, int> newMultiplicityByEdge =
                    GenerateMultiplicityByEdge(_workingSequence.GetMembersValue());     //返回一个字典，key是一个边的pair，value是边的个数
                IDictionary<int, IList<int>> graphLinkStructure =
                    GenerateGraphLinkStructure(_workingSequence.GetMembersValue());     //返回一个字典，key是基因，value是这个基因所有邻居的列表
                IDictionary<int, int> multiplicityByNodeID = GetMultiplicityByNodeID();     //返回一个字典，key是基因，value是基因的个数
                IList<IList<int>> simplePaths = _graphTool.GetSimplePath(graphLinkStructure, newMultiplicityByEdge, multiplicityByNodeID);  //获得多条简单路径
                foreach (IList<int> path in simplePaths)
                    if (path.Count < smoothingThreshold && _graph[path[0]].Count > 1)       //只有小于2个点的基因才可以平滑
                    {
                        IList<int> smooth = _graphTool.Smooth(path, ref _graph);        //path可分才返回path，不然返回null
                        foreach (int i in smooth)
                            if (!splitNodes.Contains(i))
                                splitNodes.Add(i);
                    }
                //ProcessPalindrome(_workingSequence);
                _graphTool.ProcessPalindrome(ref _workingSequence,ref  _graph);
            }
            return colorEdges;
        }

        /// <summary>
        /// Generate the graph structure: mapping from a node to a list of its neighbors
        /// </summary>
        /// <param name="sequence">an Eulerian sequence through the graph</param>
        /// <returns></returns>
        private static IDictionary<int, IList<int>> GenerateGraphLinkStructure(IList<int> sequence)
        {
            IDictionary<int, IList<int>> graphLinkStructure = new Dictionary<int, IList<int>>();
            for (int i = 0; i < sequence.Count -1; i++)
            {
                if (graphLinkStructure.ContainsKey(sequence[i]))
                {
                    if (!graphLinkStructure[sequence[i]].Contains(sequence[i + 1]))
                        graphLinkStructure[sequence[i]].Add(sequence[i + 1]);
                }
                else
                    graphLinkStructure.Add(sequence[i], new List<int> {sequence[i + 1]});
                if (graphLinkStructure.ContainsKey(sequence[i + 1]))
                {
                    if (!graphLinkStructure[sequence[i + 1]].Contains(sequence[i]))
                        graphLinkStructure[sequence[i + 1]].Add(sequence[i]);
                }
                else
                    graphLinkStructure.Add(sequence[i + 1], new List<int> {sequence[i]});
            }
            return graphLinkStructure; 
        }

        /// <summary>
        /// Generate a mapping from NodeValue to all nodes that has that value. 
        /// </summary>
        /// <param name="sequence">An Eulerian sequences of programmed Nodes</param>
        /// <returns></returns>
        private static IDictionary<int, IList<Node<int>>> GenerateGraph(SimpleLinkList<int> sequence)
        {
            IDictionary<int, IList<Node<int>>> nodeListByValue = new Dictionary<int, IList<Node<int>>>();
            foreach (Node<int> node in sequence.GetMembers())               //反射获取sequence的成员是个list的keyValue对,遍历这个list
                if (nodeListByValue.ContainsKey(node.Value))            //键是node的值，也就是sequence的值，就是基因
                    nodeListByValue[node.Value].Add(node);             //字典把相同基因，加入list变成一条链
                else
                    nodeListByValue.Add(node.Value, new List<Node<int>> {node});

            Console.Out.WriteLine("Nodes: "+ nodeListByValue.Count);  //统计一共多少基因
            return nodeListByValue; 

        }


        /// <summary>
        /// Generate a mapping from an edge to its multiplicity
        /// </summary>
        /// <param name="sequence">an Eulerian sequence through the graph</param>
        /// <returns></returns>
        private static IDictionary<Pair<int>, int> GenerateMultiplicityByEdge(IList<int> sequence)
        {
            IDictionary<Pair<int>, int> multiplicityByEdge = new Dictionary<Pair<int>, int>();
            for (int i = 0; i < sequence.Count-1; i++)
            {
                Pair<int> edge = new Pair<int>(sequence[i],sequence[i+1]);      //变成边界
                if (multiplicityByEdge.ContainsKey(edge))
                    multiplicityByEdge[edge]++;
                else
                    multiplicityByEdge.Add(edge, 1);
            }

           
            return multiplicityByEdge; 
        }


        /// <summary>
        /// Get the simple paths of the graph and their corresponding multiplicity
        /// <param name="correspondingMultiplicities"></param>
        /// <returns>return a list of simple paths</returns>
        public IList<IList<int>> GetSimplePath(out IList<int> correspondingMultiplicities)
        {
            IDictionary<int, IList<int>> graphLinkStructure =
                GenerateGraphLinkStructure(_workingSequence.GetMembersValue());
            IDictionary<Pair<int>, int> multiplicityByEdge =
                GenerateMultiplicityByEdge(_workingSequence.GetMembersValue());
            IDictionary<int, int> multiplicityByNodeID = GetMultiplicityByNodeID();
            IList<IList<int>> syntenyBlocks = _graphTool.GetSimplePath(graphLinkStructure, multiplicityByEdge, multiplicityByNodeID);
            correspondingMultiplicities = new List<int>();
            foreach (IList<int> syntenyBlock in syntenyBlocks)
                correspondingMultiplicities.Add(_graph[syntenyBlock[0]].Count);
            return syntenyBlocks;   
        }

        /// <summary>
        /// Get the mapping of a node and its multiplicity
        /// </summary>
        /// <returns>a dictionary contains this structure</returns>
        private IDictionary<int, int> GetMultiplicityByNodeID()
        {
            IDictionary<int, int> multiplicityByNodeID = new Dictionary<int, int>();
            foreach (KeyValuePair<int, IList<Node<int>>> pair in _graph)
                multiplicityByNodeID[pair.Key] = pair.Value.Count;
            return multiplicityByNodeID;
        }

        /// <summary>
        /// Propagate the color of the skeleton color throught the ABruijn Graph  
        /// </summary>
        /// <param name="blockColors"> A list of Blocks. The color ID of each block is also its its position in the list </param>
        /// <param name="propagationRadius"> The maximum radius that the color can propagate </param>
        /// <returns>a mapping between nodeids and their colors</returns>
        public IDictionary<int, int> PropagateSkeletonColor(IList<IList<int>> blockColors, int propagationRadius)
        {

            SimpleLinkList<int> origialSequence = new SimpleLinkList<int>();
            origialSequence.AddList(_sequence);
            IDictionary<int, IList<Node<int>>> graph = GenerateGraph(origialSequence);

            IDictionary<int, int> colorByNodeID = new Dictionary<int, int>();
            //initially, all nodes are uncolored ( -1)
            foreach (int i in _sequence)
                colorByNodeID[i] = -1;

            //add colors
            for (int i = 0; i < blockColors.Count; i++)
                foreach (int nodeID in blockColors[i])
                    colorByNodeID[nodeID] = i;
            HashSet<int> unColorNodes = new HashSet<int>();
            foreach (KeyValuePair<int, int> pair in colorByNodeID)
                if (pair.Value == -1)
                    unColorNodes.Add(pair.Key);
            for (int i = 0; i < propagationRadius; i++)
            {
                for (int j = 0; j < blockColors.Count; j++)
                {
                    HashSet<int> modifiedUnColoredNodes = new HashSet<int>(unColorNodes);
                    foreach (int node in modifiedUnColoredNodes)
                    {
                        int newColor = FindDominantColor(node,colorByNodeID, graph);
                        if (newColor != -1)
                        {
                            colorByNodeID[node] = newColor;
                            unColorNodes.Remove(node);
                        }
                    }
                }
            }
            return colorByNodeID; 
        }


        /// <summary>
        /// Find the dominant color of the neighbors of a node
        /// </summary>
        /// <param name="node">currentNode</param>
        /// <param name="colorByNodeID">Mapping from nodeID to colorID</param>
        /// <param name="graph"></param>
        /// <returns>the dominant color </returns>
        private static int FindDominantColor(int node, IDictionary<int, int> colorByNodeID, IDictionary<int, IList<Node<int>>> graph)
        {
            HashSet<int> neighborNodes = new HashSet<int>();

            IList<Node<int>> allMergedNodes;
            graph.TryGetValue(node, out allMergedNodes);
            foreach (Node<int> singleNode in allMergedNodes)
            {
                if (singleNode.Next != null)
                    if (!neighborNodes.Contains(singleNode.Next.Value))
                        neighborNodes.Add(singleNode.Next.Value);
                if (singleNode.Previous != null)
                    if (!neighborNodes.Contains(singleNode.Previous.Value))
                        neighborNodes.Add(singleNode.Previous.Value);
            }
            IDictionary<int, int> colorMembersByColorID = new Dictionary<int, int>();
            foreach (int i in neighborNodes)
            {
                int colorID = colorByNodeID[i];
                if (colorMembersByColorID.ContainsKey(colorID))
                    colorMembersByColorID[colorID] = colorMembersByColorID[colorID] + 1;
                else
                    colorMembersByColorID[colorID] = 1;

            }
            //find dominant colorID;
            int maxNumber = 0;
            int maxColorID = 0; ;
            foreach (KeyValuePair<int, int> pair in colorMembersByColorID)
            {

                if (pair.Value > maxNumber && pair.Key != -1)
                {
                    maxNumber = pair.Value;
                    maxColorID = pair.Key;
                }
            }
            return maxColorID; 
        }
    }
}