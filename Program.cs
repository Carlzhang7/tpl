using System.IO;
using System;
using System.Threading.Tasks.Dataflow;    //TPL简化数据流所需库
using System.Threading.Tasks;
using TPL_finder;
using Spectre.Console;
using System.Threading;
using Serilog;
using CommandDotNet;





namespace TPL_finder
{
    public class TPL_sensitive_finder
    {
        [Command(Name="TPL",
        Usage="replace <String> <String>",
        Description="find sensitive words",
        ExtendedHelpText="more details and examples")]
        public void process(string[] args)
        {
            
            
            String a ;

            a= AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("[green]input string or read file[/]")
            .PageSize(5)
            .MoreChoicesText("[grey](reveal more datastream)[/]")
            .AddChoices(new[] {
                "string", "file",
            }));
            
            AnsiConsole.Progress()
                .Start(ctx=>
                {
            
                    var task1=ctx.AddTask("[bold red]Please wait patiently[/]");
                    while(!ctx.IsFinished)
                    {
                        task1.Increment(1.5);
                        Thread.Sleep(10);
                    }
                });
             
            var table=new Table();
            table.AddColumn("date");
            table.AddColumn("auther");
            table.AddColumn("version");
            table.AddRow("2021.11.25","Carl","1.0");
            table.Border(TableBorder.Ascii);
            AnsiConsole.Write(table);
          
            sensitive_finder find=new sensitive_finder("Hello world","Hello Carl");  
            String line;
            
            if(a=="string")
            {
                Log.Information("Please input a string to be transformed");
                line = Console.ReadLine();
                Task.Run(() =>    //开启线程
                {
                    find.transformBlock.Post(line);  //将输入内容送至transform block
                });
            }
            else
            {
                Log.Information("{Filepath} is handled by default","input.txt");
                Task.Run(() =>    
                {
                    try{
                        using(StreamReader sr = new StreamReader("test.txt"))

                        while ((line= sr.ReadLine())!= null)
                        {
                        
                            find.transformBlock.Post(line); 
                        }
                    }
                    catch
                    {
                        Log.Error("Can't read {FilePath}", "test.txt");
                    }
                });
            }
        }
    }

    public  class sensitive_finder 
    {
        
        public string original;  //原始词
        public string substitude;   //替换词
        public TransformBlock<string, string> transformBlock;    //转化处理模块
        public ActionBlock<string> consoleBlock;        //命令行输出模块
        public ActionBlock<string> fileBlock;       //文件输出模块
        public BroadcastBlock<string> broadcastBlock;       //内容传递分发模块

        
        public sensitive_finder(String ori,String sub)
        {
           
            this.original=ori;
            this.substitude=sub;
            
            transformBlock = new TransformBlock<string, string>((input) =>
            {
                return input.Replace(this.original, this.substitude);       //return处理结果
            });
            Log.Debug("transformBlock has been created");
            consoleBlock = new ActionBlock<string>((input) =>
            {
                Console.WriteLine(input);       //终端显示结果
            });
            Log.Debug("consoleBlock has been created");
            fileBlock = new ActionBlock<string>((input) =>
            {
                try{
                    using (StreamWriter sw = new StreamWriter("output.txt",true))
                    {   
                        sw.WriteLine(input);     //输出至文件        
                    }  
                }
                catch{
                    Log.Error("can not write to {Filepath}","output.txt");

                }

            });
            Log.Debug("fileBlock has been created");
            broadcastBlock = new BroadcastBlock<string>(p=>p);      //保存一个缓冲值
            Log.Debug("broadBlock has been created");
            try{
                transformBlock.LinkTo(broadcastBlock);      //链接处理和缓冲模块
                broadcastBlock.LinkTo(consoleBlock);        //链接缓冲和两个输出模块
                broadcastBlock.LinkTo(fileBlock);
            }
            catch{
                Log.Error("can not link every blocks");
            }
            
            Log.Debug("every Block has been linked");
            Log.Information("The TPL sensitive word finder is ready!");
        }
    }
}

namespace sensitive_word_finder
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            TPL_sensitive_finder ff=new TPL_sensitive_finder();
            ff.process(args);
            Console.ReadLine();
            Log.CloseAndFlush();
        }
    }
}
