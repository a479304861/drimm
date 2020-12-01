using System;

namespace SyntenyFast
{
    internal static class Program
    {
        private const global::System.String INFILE = "G:/桌面/毕设/drimm_input.sequence";
        private const global::System.String OUTDIR = "G:/桌面/毕设/output";

        private static void Main(string[] args)
        {
            
            int cycleLengthThreshold = 30;          //环长度          
       
            string infile = INFILE;
     
            string outdir = OUTDIR;
       
            int dustLengthThreshold = 8;    

            int simplificationSteps = 15;        //步数15


            IGraphTool graphTool = new GraphTool();         //创建图池子
            ABruijnGraph aBruijnGraph = new ABruijnGraph(graphTool);    //AB图
            IDataReader dataReader = new SyntenyDataReader(infile, ' ');
            IDataWriter dataWriter = new SyntenyDataWriter(outdir+"/synteny.txt", ' ', outdir+"/sequenceColor.txt", infile,
                                                           outdir+"/modifiedSequence.txt");
            IColorTracker colorTracker = new ColorTracker();        //颜色标记
            ISequenceSmother smother = new SequenceSmother(2, cycleLengthThreshold);    //序列平滑
            ISyntenyFinder syntenyFinder = new SyntenyFinder(dataReader, dataWriter, aBruijnGraph, smother, colorTracker);  //核心函数
            syntenyFinder.Run(cycleLengthThreshold, 2, 3, simplificationSteps, true, dustLengthThreshold,outdir);
        }
    }
}
        