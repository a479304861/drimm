using System;
using System.IO;

namespace SyntenyFast
{
    internal static class Program
    {
        private const global::System.String OUTDIR = "G:/桌面/毕设/output";
        private const global::System.String INPUT = "E:/学习资料/IjDemo/graduate_project/file";

        private static void Main(string[] args)
        {

            if (args.Length != 4)
            {
                Console.Out.Write("参数不正确");
                return;
            }
            int cycleLengthThreshold = 20;          //环长度     
            int dustLengthThreshold = 20;
            string infile = args[0];
            string outdir = args[1];
           
            if (!Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }
            cycleLengthThreshold =int.Parse(args[2]);
            dustLengthThreshold= int.Parse(args[3]);

            //步数15
            /* String infile = INPUT + '/'+ "801097480652455936.sequence";
             String outdir = OUTDIR + '/' + "801097480652455936";
             int cycleLengthThreshold = 20;

             int dustLengthThreshold = 20;
             if (!Directory.Exists(outdir))
             {
                 Directory.CreateDirectory(outdir);
             }*/
            int simplificationSteps = 15;
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
        