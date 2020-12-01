using System;
using System.Collections.Generic;
using System.IO;
using Util.Collection;

namespace SyntenyFast
{
    public class SyntenyDataWriter:IDataWriter
    {
        private readonly char __separator;
        private readonly StreamWriter _syntenyWriter;
        private readonly StreamWriter _sequenceWriter;
        private readonly StreamReader _inputReader;
        private readonly StreamWriter _modifiedWriter;
        public SyntenyDataWriter(string syntenyFileName, char _separator, string sequenceFileName, string inputFile, string modifiedSequenceFileName)
        {
            __separator = _separator;
            _syntenyWriter = new StreamWriter(syntenyFileName);
            _sequenceWriter = new StreamWriter(sequenceFileName);
            _inputReader = new StreamReader(inputFile);
            _modifiedWriter = new StreamWriter(modifiedSequenceFileName);

        }
        /// <summary>
        /// Write the consensus synteny blocks and its multiplicity
        /// </summary>
        /// <param name="multiplicity"></param>
        /// <param name="consensusPath"></param>
        /// <returns></returns>
        public void WriteSyntenyConsensus(IList<int> multiplicity, IList<IList<int>> consensusPath,
            IDictionary<int, IList<IList<Node<int>>>> synNodeListBySynId, IDictionary<Node<int>, Node<int>> workToSource,String outdir)
        {
            if (multiplicity.Count != consensusPath.Count)
                throw new ArgumentException("data invalid");
           
            IList<IList<int>> sourcePath = new List<IList<int>>();
            for(int i = 0; i < consensusPath.Count; i++)
            {
                List<int>sourseTemp = new List<int>();
                if (!synNodeListBySynId.ContainsKey(i))
                {
                    sourcePath.Add(sourseTemp);
                    continue;
                }
                IList<Node<int>> nodes = synNodeListBySynId[i][0];
                int flag = 1;
                for(int j = 0; j < nodes.Count; j++)
                {
                    if (workToSource.ContainsKey(nodes[j]))
                    {
                        Node<int> sourceNode = workToSource[nodes[j]];
                        sourseTemp.Add(sourceNode.Value);
                        
                    }
                    if (j > consensusPath[i].Count)
                    {
                        continue;
                    }
                    if(nodes[j].Value!= consensusPath[i][j])
                    {
                       flag = 0;
                    }
                }
                if (flag == 0)
                {
                    sourseTemp.Reverse();
                }
              
                sourcePath.Add(sourseTemp);
            }
         
            for (int i = 0; i < consensusPath.Count; i++)
            {
                _syntenyWriter.Write( i + ":"+  multiplicity[i] + __separator.ToString());
              
                for(int j = 0; j < consensusPath[i].Count; j++)
                {
                    _syntenyWriter.Write(consensusPath[i][j] + __separator.ToString());
                }
                
                _syntenyWriter.WriteLine();
               
            }
            StreamWriter sw2 = new StreamWriter(outdir + "/sourceSynteny.txt");
            for (int i = 0; i < sourcePath.Count; i++)
            {
                sw2.Write(i + ":" + multiplicity[i]+ __separator.ToString());
                foreach(int node in sourcePath[i])
                {
                    sw2.Write(node + __separator.ToString());
                }
                sw2.WriteLine();
            }
            _syntenyWriter.Flush();
            _syntenyWriter.Close();
            sw2.Flush();
            sw2.Close();
            return; 

        }

        /// <summary>
        /// Write the sequence with color.
        /// </summary>
        /// <param name="sequence">sequence</param>
        /// <param name="listInColor">sequence of color</param>
        public void WriteSequenceWithColor(IList<int> sequence, IList<int> listInColor)
        {
            IList<int> sequencesLength = new List<int>();
            string line;
            while ((line = _inputReader.ReadLine())!= null)
            {
                line = line.Trim(__separator);
                string[] numbers = line.Split(__separator);
                sequencesLength.Add(numbers.Length); //TODO devide this number in to 2 ; Just temporary , since the format of the output is still old. 
            }
            int sequenceID = 0;
            int j = 0;
            //remove all padding numbers
           /* IList<int> paddingFreeSequence = new List<int>();
            for (int i = 0; i < sequence.Count; i++)
                if (sequence[i] >= -2) //begin and end of the sequence were added with -1 and -2 to get rid of the null care!!!
                    paddingFreeSequence.Add(sequence[i]);
           */
            //TODO this is the source of error. 
            for (int i = 0; i < sequence.Count ; i++)
            {
                if (sequenceID == sequencesLength.Count)
                    break; 
                if (j == sequencesLength[sequenceID])
                {
                    _sequenceWriter.WriteLine();
                    sequenceID++;
                    j = 0; 
                }
                if (sequence[i] >= 0)
                {
                    _sequenceWriter.Write(sequence[i] + __separator.ToString() + listInColor[i] + __separator);
                    j++;
                }
            }
            _sequenceWriter.Flush();
            _sequenceWriter.Close();

        }

        /// <summary>
        /// Write the modified sequence to a file
        /// </summary>
        /// <param name="sequence"></param>
        public void WriteModifiedSequence(IList<int> sequence)
        {
            bool isNewLine = false;
            sequence.RemoveAt(0);
            sequence.RemoveAt(sequence.Count-1);
            foreach (int i in sequence)
            {
                if (!isNewLine && i < 0)
                {
                    _modifiedWriter.WriteLine();
                    isNewLine = true; 
                }
                if (i < 0 && isNewLine )
                    continue;

                _modifiedWriter.Write(i+" ");
                isNewLine = false; 

            }
            _modifiedWriter.Flush();
            _modifiedWriter.Close();
        }

        public void WriteBlocksSign(IList<IList<int>> blockSign,string outdir)
        {
            StreamWriter sw = new StreamWriter(outdir+"/blocks.txt");
            foreach (IList<int> sequence in blockSign)
            {
                foreach (int i in sequence)
                {
                    sw.Write(i+" ");
                }
                sw.WriteLine();
            }
            sw.Flush();
            sw.Close();
        }

        public void WriteSplit(HashSet<int> splitNodeGlobal, string outdir)
        {
            StreamWriter sw = new StreamWriter(outdir + "/split.txt");        //所有被split的点都写入split.txt中
            foreach (int i in splitNodeGlobal)
                sw.WriteLine(i);
            sw.Flush();
            sw.Close();
          
        }
        // 912:(995,995,35,234) (10408,10408,35,9) 
        // 
       public void WriteMultiplySyn(IDictionary<int, IList<IList<Node<int>>>> synNodeListBySynId, IDictionary<Node<int>, Node<int>> workToSource, IDictionary<Node<int>, Pair<int>> nodeToIndex, string outdir)
        {
            StreamWriter sw = new StreamWriter(outdir + "/MultiplySyn.txt");
            IList<IList<IList<Node<int>>>> synNodeListListBySynId = new List<IList<IList<Node<int>>>>();
            IDictionary< IList < IList < Node<int> >>, int> synNodeListIndex = new Dictionary<IList<IList<Node<int>>>, int>();
            foreach (KeyValuePair<int,  IList<IList<Node<int>>>> kv in synNodeListBySynId)
            {
                synNodeListListBySynId.Add(kv.Value);
                synNodeListIndex.Add(kv.Value,kv.Key);
            }
            ((List<IList<IList<Node<int>>>>)synNodeListListBySynId).Sort((a, b) => synNodeListIndex[a].CompareTo(synNodeListIndex[b]));
            for(int i=1;i< synNodeListListBySynId.Count; i++)
            {
                foreach (IList<Node<int>> synNodeListList in synNodeListListBySynId[i])
                {
                    sw.Write(synNodeListIndex[synNodeListListBySynId[i]] + ":");
                    foreach (Node<int>  synNode in synNodeListList)
                    {
                        if (workToSource.ContainsKey(synNode))
                        {
                            Node<int> sourceNode = workToSource[synNode];
                            if (nodeToIndex.ContainsKey(sourceNode))
                            {
                                Pair<int> index = nodeToIndex[sourceNode];
                                sw.Write( "("+synNode.Value+","+ sourceNode.Value+","+index.First+ ","+ index.Second+") ");
                            }
                        }
                    }
                    sw.WriteLine();
                }
            }
            sw.Flush();
            sw.Close();
            StreamWriter sw2 = new StreamWriter(outdir + "/sourceSynteny.txt");
            for (int i=1;i< synNodeListListBySynId.Count; i++)
            {
                IList<Node<int>> synNodeListList = synNodeListListBySynId[i][0];
                sw2.Write(synNodeListIndex[synNodeListListBySynId[i]] + ": ");
                    foreach (Node<int>  synNode in synNodeListList)
                    {
                        if (workToSource.ContainsKey(synNode))
                        {
                            Node<int> sourceNode = workToSource[synNode];
                        sw2.Write(sourceNode.Value+" ");
                        }
                    }
                sw2.WriteLine();
            }
            sw2.Flush();
            sw2.Close();



        }
    }
}