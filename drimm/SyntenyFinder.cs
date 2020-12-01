using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Util.Collection;

namespace SyntenyFast
{
    public class SyntenyFinder : ISyntenyFinder
    {
        private readonly IABruijnGraph _aBruijnGraph;
        private readonly IDataReader _dataReader;
        private readonly IDataWriter _dataWriter;
        private readonly ISequenceSmother _sequenceSmother;
        private readonly IColorTracker _colorTracker;

        public SyntenyFinder(IDataReader dataReader, IDataWriter dataWriter, IABruijnGraph aBruijnGraph, ISequenceSmother sequenceSmother, IColorTracker colorTracker)
        {
            _aBruijnGraph = aBruijnGraph;
            _dataReader = dataReader;
            _dataWriter = dataWriter;
            _sequenceSmother = sequenceSmother;
            _colorTracker = colorTracker;
        }

        /**
         *     20  2  3  15  true  8
         *
         * */

        public void Run(int cycleLenghtThreshold, int smoothingThreshold, int propagationRadius, int simplificationSteps, bool smooth, int dustThreshold,string outdir)
        {
            IList<int> sequence = _dataReader.ReadSequences(cycleLenghtThreshold);  //读序列存在sequence中
            _sequenceSmother.RemoveDust(ref sequence, dustThreshold);       //将基因个数大于repeatThreshold的去除
            _aBruijnGraph.ThreadSequence(sequence);
            HashSet<int> splitNodeGlobal = new HashSet<int>();
            for (int i = 0; i < simplificationSteps; i++){
                bool shouldSmooth = false;  
                if(smooth)
                    shouldSmooth = i == simplificationSteps - 2;
                if (simplificationSteps == 1)
                    shouldSmooth = true;//for test pass
                HashSet<int> splitNodes;
                HashSet<KeyValuePair<int, int>> edgesSet = _aBruijnGraph.Simplify(cycleLenghtThreshold, smoothingThreshold, shouldSmooth,out splitNodes);
                if(edgesSet != null && edgesSet.Count!= 0)
                    _colorTracker.AppendColorEdges(edgesSet);       //把edgesSet里面的点存起来
                if (splitNodes.Count != 0)
                    splitNodeGlobal.UnionWith(splitNodes);      //取并集
            }
            IList<int> multiplicities;
            IList<IList<int>> simplePaths = _aBruijnGraph.GetSimplePath(out multiplicities);
            IDictionary<IList<int>, int> multiplicityBySimplePathList = new Dictionary<IList<int>, int>();
            //sort the simplePath by its multiplicity
            for (int i = 0; i < simplePaths.Count; i++)
                multiplicityBySimplePathList.Add(simplePaths[i], multiplicities[i]);        //key路径，value所对应的个数
            ((List<IList<int>>)simplePaths).Sort((a,b)=> multiplicityBySimplePathList[a].CompareTo(multiplicityBySimplePathList[b]));
            ((List<int>) multiplicities).Sort();
            //write the splitNodeGlobal

            IList<int> modifiedSequence = _aBruijnGraph.GetModifiedSequence();      //获得修改后的sequence
            IDictionary<int, int> colorByNodeID = _colorTracker.BackTracking(simplePaths); //_aBruijnGraph.PropagateSkeletonColor(simplePaths,propagationRadius);
            IList<int> smoothColor = _sequenceSmother.Smooth(sequence, colorByNodeID);
            IList<int> listColors = _sequenceSmother.ReStoreDust(ref sequence, smoothColor);
            IList<IList<int>> blocksSign = _sequenceSmother.GetBlocksSign(modifiedSequence, simplePaths, 2);
            IList<Node<int>> modifiedNodeSequence = _aBruijnGraph.GetModifiedNodeSequence();
            IDictionary<Node<int>, Node<int>> workToSource = _aBruijnGraph.GetWorkToSource();
            IDictionary<int, IList<IList<Node<int>>>> SynNodeListBySynId = _sequenceSmother.getSynNodeListBySynId(modifiedNodeSequence, simplePaths);
 
       
            _dataWriter.WriteSplit(splitNodeGlobal,outdir);
            _dataWriter.WriteSyntenyConsensus(multiplicities, simplePaths,SynNodeListBySynId,workToSource,outdir);
            _dataWriter.WriteSequenceWithColor(sequence, listColors);
            _dataWriter.WriteModifiedSequence(modifiedSequence);
            _dataWriter.WriteBlocksSign(blocksSign, outdir);

        }
    }
}