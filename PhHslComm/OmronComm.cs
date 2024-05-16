using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Profinet.Omron;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections;
using Grpc.Core;
using static Arp.Plc.Gds.Services.Grpc.IDataAccessService;
using Arp.Plc.Gds.Services.Grpc;
using Grpc.Net.Client;
using static PhHslComm.GrpcTool;
using System.Net.Sockets;
using System.Drawing;
using Opc.Ua;
using NPOI.SS.Formula.Functions;
using HslCommunication.LogNet;
using Microsoft.Extensions.Logging;


namespace PhHslComm
{

    #region 从Excel中抽象出来的数据类型
    public struct DeviceInfoConSturct1_CIP
    {
        public string varType;  //数据类型
        public string varName;  //标签名 不带[X]
        public string stationName;//工位名
        public int varIndex;  //数组索引
        public int stationNumber;  //新增工位对应序号
        public string varAnnotation; //描述
    }

    //电芯记忆
    public struct DeviceInfoDisStruct2_CIP
    {
        public string varType;  //变量类型
        public string varName; //标签名 不带[X]
        public string varAnnotation; //描述
        public int stationNumber;  //新增工位对应序号
        public string stationName;//工位名
    }

    //六个工位
    public struct  StationInfoStruct_CIP
    {
        public string stationName;  //工位名
        public string varType;  //变量类型
        public string varName;  //变量名
        public string varAnnotation;  //描述

    }

    public struct  OneSecInfoStruct_CIP
    {
        public string varType;  //变量类型
        public string varName;  //标签名
        public string varAnnotation;   //描述
        public int varIndex;  //数组索引

    }
    #endregion 从Excel中抽象出来的数据类型


    class OmronComm
    {

        #region 从Excel解析来的数据
        //设备信息里的离散数组数据
        public DeviceInfoConSturct1_CIP[] Auto_Process;
        public DeviceInfoConSturct1_CIP[] Clear_Manual;

        //设备信息里的离散结构体数据和数组数据
        public DeviceInfoDisStruct2_CIP[] Battery_Memory;
        public DeviceInfoDisStruct2_CIP[] BarCode;
        public DeviceInfoDisStruct2_CIP[] EarCode;

        //六大工位数据
        public StationInfoStruct_CIP[] chongmo;
        public StationInfoStruct_CIP[] reya;
        public StationInfoStruct_CIP[] dingfeng;
        public StationInfoStruct_CIP[] zuojiaofeng;
        public StationInfoStruct_CIP[] youjiaofeng;
        public StationInfoStruct_CIP[] cefeng;

        //报警信号（1000ms）
        public OneSecInfoStruct_CIP[] Sys_Manual;
        public OneSecInfoStruct_CIP[] Production_statistics;
        public OneSecInfoStruct_CIP[] Cutterused_statistics;
        public OneSecInfoStruct_CIP[] Y6;
        public OneSecInfoStruct_CIP[] Manual_Andon;

        //非报警信号 （1000ms）
        public OneSecInfoStruct_CIP[] Vacuum_Alarm;
        public OneSecInfoStruct_CIP[] Senor_Alarm;
        public OneSecInfoStruct_CIP[] Motor_POTLimit_Err;
        public OneSecInfoStruct_CIP[] Motor_NOTLimit_Err;
        public OneSecInfoStruct_CIP[] Motor_Prevent_Promt;
        public OneSecInfoStruct_CIP[] Exception_information;
        public OneSecInfoStruct_CIP[] Operation_prompt;
        public OneSecInfoStruct_CIP[] Temperature_Alarm;
        public OneSecInfoStruct_CIP[] Cylinder_Reset_Promt;
        public OneSecInfoStruct_CIP[] Motor_Reset_Promt;
        public OneSecInfoStruct_CIP[] Stopstate_Error;
        public OneSecInfoStruct_CIP[] Grating_Error;
        public OneSecInfoStruct_CIP[] Scapegoat_Error;
        public OneSecInfoStruct_CIP[] Door_Error;
        public OneSecInfoStruct_CIP[] out_power;

        #endregion 从Excel解析来的数据


        //创建Grpc实例
        public GrpcTool grpcToolInstance = new GrpcTool();   

        //CIP连接和线程
        OmronConnectedCipNet[] _cip;
        OperateResult ret;
        int totalN=0;
        Thread[] thr;


        #region 设置grpc通讯参数
        // Create options for calls made by client
        static CallOptions options1 = new CallOptions(
                new Metadata {
                        new Metadata.Entry("host","SladeHost")
                },
                DateTime.MaxValue,
                new CancellationTokenSource().Token);
        static IDataAccessServiceClient grpcDataAccessServiceClient = null;
        #endregion


        // 实例化一个日志后，就可以使用了
        const string logsFile = ("/opt/plcnext/apps/Logs.txt");
        //const string logsFile = "D:\\2024\\Work\\12-冠宇数采项目\\ReadFromStructArray\\GuanYu";
        public ILogNet logNet;

        //创建nodeID字典
        public Dictionary<string, string> nodeidDictionary;

        public OmronComm(int totalnumber)
        {
            //一个CIP client对应一个thread

            logNet = new LogNetSingle(logsFile);

            totalN = totalnumber;
 
            _cip = new OmronConnectedCipNet[totalN];
            thr = new Thread[totalN];
            for (int i = 0; i < totalnumber; i++)
            {
                _cip[i] = new OmronConnectedCipNet("192.168.1.31");  //每个Client绑定的ip都是这个
            }

            #region 从xml获取nodeid,保存到对应的类，注意xml中的别名要和对应类的属性名一致 调用构造函数时 执行一次

            try
            {
                const string filePath = "/opt/plcnext/apps/GrpcSubscribeNodes.xml";
                //const string filePath = "D:\\2024\\Work\\12-冠宇数采项目\\ReadFromStructArray\\PhHslComm\\PhHslComm\\GrpcSubscribeNodes\\GrpcSubscribeNodes.xml";

                nodeidDictionary = grpcToolInstance.getNodeIdDictionary(filePath);  //将xml中的值写入字典中
                //int count = nodeidDictionary.Count;
            }
            catch(Exception e)
            {
                logNet.WriteError("Error:"+ e); 
            }
 
            #endregion     

        }

        public async void CipConnect()

        {
            #region 建立CIP连接    //假如稳态运行后，CIP连接掉线，该如何处理 TODO
            for (int i = 0; i < totalN; i++)
            {
                //var retIn = await _cip[i].ConnectServerAsync(); //长连接
                var retIn = _cip[i].ConnectServer();
                if (retIn.IsSuccess)
                {
                    //logNet.WriteInfo("num " + i.ToString() + " connect success!");
                    Console.WriteLine("num {0} connect success!", i);
                }
                else
                {
                    //logNet.WriteError("num " + i.ToString() + "connect failed!");
                    Console.WriteLine("num {0} connect failed!", i);

                }
            }
            #endregion

        }
        public async void GrpcConnect()
        {
            #region 建立Grpc连接  // //假如稳态运行后，Grpc连接掉线，该如何处理 TODO
            var udsEndPoint = new UnixDomainSocketEndPoint("/run/plcnext/grpc.sock");
            var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
            var socketsHttpHandler = new SocketsHttpHandler
            {
                ConnectCallback = connectionFactory.ConnectAsync
            };
            // Create a gRPC channel to the PLCnext unix socket
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpHandler = socketsHttpHandler
            });
            grpcDataAccessServiceClient = new IDataAccessService.IDataAccessServiceClient(channel);// Create a gRPC client for the Data Access Service on that channel
            #endregion
 
        }

        //创建对应线程
        public void startThr()
        {
            //读1000ms数据
            thr[0] = new Thread(() =>
            {
                this.ReadOnceSecInfo(_cip[0]);
            });
            thr[0].Start();


            ////读设备信息
            thr[1] = new Thread(() =>
            {
                this.ReadDeviceInfo(_cip[1]);
            });
            thr[1].Start();


            //读六大工位信息
            thr[2] = new Thread(() =>
            {
                this.ReadStationInfo1(_cip[2]);
            });
            thr[2].Start();
            thr[3] = new Thread(() =>
            {
                this.ReadStationInfo2(_cip[3]);
            });
            thr[3].Start();
            thr[4] = new Thread(() =>
            {
                this.ReadStationInfo3(_cip[4]);
            });
            thr[4].Start();

            //thr[0].Join();

        }


        #region Thread1 读1000ms数据
        //读 “报警信号”子表的信息，二维数组待确认，放卷R轴.out_power 这种变量需要单独建立结构体，那这个每个单独的结构体里的total数量都写整个的数量
        public void ReadOnceSecInfo(OmronConnectedCipNet cip)
        {
            while (true)
            {
                //cip.ConnectServer();
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);

                //读取报警信号
                OperateResult<bool> ret = cip.ReadBool("Y6[5]");   
                if (ret.IsSuccess)
                {
                    if(ret.Content == true)
                    {
                        ReadOneSecData(Vacuum_Alarm , cip);
                        ReadOneSecData(Senor_Alarm, cip);
                        ReadOneSecData(Motor_POTLimit_Err, cip);
                        ReadOneSecData(Motor_NOTLimit_Err, cip);
                        ReadOneSecData(Motor_Prevent_Promt, cip);
                        ReadOneSecData(Exception_information, cip);
                        ReadOneSecData(Operation_prompt,cip);
                        ReadOneSecData(Temperature_Alarm,cip);
                        ReadOneSecData(Cylinder_Reset_Promt, cip);
                        ReadOneSecData(Motor_Reset_Promt, cip);
                        ReadOneSecData(Stopstate_Error, cip);
                        ReadOneSecData(Grating_Error,cip);
                        ReadOneSecData(Scapegoat_Error, cip);
                        ReadOneSecData(Door_Error, cip);
                        ReadOneSecData(out_power,cip);   
                    }
                    else
                    {
                        //logNet.WriteInfo("No Warning");
                        Console.WriteLine("No Warning");
                    }
                   
                }
                else
                {
                    //logNet.WriteError("Y6[5] read failed");
                    Console.WriteLine("Y6[5] read failed");
                }


                //读取非报警信号
                ReadOneSecData(Sys_Manual,cip);
                ReadOneSecData(Y6, cip);
                ReadOneSecData(Manual_Andon, cip);
                ReadOneSecData(Production_statistics, cip);
                ReadOneSecData(Cutterused_statistics, cip);


                //计算从开始读到读完的时间
                TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
                DateTime nowDisplay = DateTime.Now;
                TimeSpan dur = (end - start).Duration();

                logNet.WriteInfo("Thread ReadOnceSecInfo read time : " + (dur.TotalMilliseconds).ToString());
                //Console.WriteLine("Thread ReadOnceSecInfo read time:{0} read Duration:{1}", nowDisplay.ToString("yyyy-MM-dd HH:mm:ss:fff"), dur.TotalMilliseconds);


                if (dur.TotalMilliseconds < 100)
                {
                    int sleepTime = 1000 - (int)end.TotalMilliseconds;
                    Thread.Sleep(sleepTime);
                }

            }
        }
        #endregion Thread1 


        #region Thread2 读设备信息
        public void ReadDeviceInfo(OmronConnectedCipNet cip)
        {
            
            while (true)
            {
                string[] AllDeviceInformation;
                AllDeviceInformation = new string[75];
                for (int i = 0; i < AllDeviceInformation.Length; i++)
                {
                    AllDeviceInformation[i] = ""; // 将每个元素初始化为空字符串
                }

                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);

                ReadDeviceInfoConSturct1(Auto_Process, cip, AllDeviceInformation);

                ReadDeviceInfoConSturct1(Clear_Manual, cip, AllDeviceInformation);
                ReadDeviceInfoDisStruct2(Battery_Memory,cip, AllDeviceInformation);

                ReadDeviceInfoDisStruct2(BarCode, cip, AllDeviceInformation);
                ReadDeviceInfoDisStruct2(EarCode, cip, AllDeviceInformation);

                for (int i= 1; i <AllDeviceInformation.Length; i++)
                {
                    string StationName_Now = (i).ToString() ;
                    //Console.WriteLine("{0}", StationName_Now);

                    // AllDeviceInformation 为字符串数组，每一个元素都对应一个工位的值 
                    #region Grpc发送给IEC

                    var listWriteItem = new List<WriteItem>();
                    WriteItem[] writeItems = new WriteItem[] { };
                    writeItems = null;

                    listWriteItem.Clear();

                    try
                    {
                        listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary[StationName_Now], Arp.Type.Grpc.CoreType.CtString, AllDeviceInformation[i]));
                        //Console.WriteLine("{0}", AllDeviceInformation[i]);
                        //Console.WriteLine(nodeidDictionary[StationName_Now]);
                    }
                    catch (Exception e)
                    {
          
                        Console.WriteLine("ERRO: {0}，{1}", e,nodeidDictionary.GetValueOrDefault(StationName_Now));
                    }

                    SendDataToIEC(listWriteItem);
                    #endregion 

                }

                TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
                DateTime nowDisplay = DateTime.Now;
                TimeSpan dur = (start - end).Duration();


                logNet.WriteInfo("Thread ReadDeviceInfo read time : " + (dur.TotalMilliseconds).ToString());
                //Console.WriteLine("Thread ReadDeviceInfo read time:{0} read Duration:{1}", nowDisplay.ToString("yyyy-MM-dd HH:mm:ss:fff"), dur.TotalMilliseconds);


                if (dur.TotalMilliseconds < 100)
                {
                    int sleepTime = 100 - (int)end.TotalMilliseconds;
                    Thread.Sleep(sleepTime);
                }

            }
        }
        #endregion Thread2 读设备信息


        #region Thread3 读工位信息
        public void ReadStationInfo1(OmronConnectedCipNet cip)
        {
            while (true)
            {
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);

                //根据Auto_Process的值，判断是否需要读取对应工位的数据

                OperateResult<int>  ret = cip.ReadInt32("Auto_process[32]");
                if (ret.IsSuccess)
                {
                    if (ret.Content < 50 && ret.Content >= 20) ReadStation(reya, cip);
                }
                else
                {
                    logNet.WriteError("Auto_process[32] read failed");
                    //Console.WriteLine("Auto_process[32] read failed");
                }

                TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
                DateTime nowDisplay = DateTime.Now;
                TimeSpan dur = (start - end).Duration();

                //logNet.WriteInfo("Thread ReadStationInfo1 read time : " + (dur.TotalMilliseconds).ToString());
                Console.WriteLine("Thread ReadStationInfo1 read time:{0} read Duration:{1}", nowDisplay.ToString("yyyy-MM-dd HH:mm:ss:fff"), dur.TotalMilliseconds);


                if (dur.TotalMilliseconds < 100)
                {
                    int sleepTime = 100 - (int)dur.TotalMilliseconds;
                    Thread.Sleep(sleepTime);
                }
            }

        }
        #endregion Thread3 读工位信息

        #region Thread4 读工位信息
        public void ReadStationInfo2(OmronConnectedCipNet cip)
        {
            while (true)
            {
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);

                //根据Auto_Process的值，判断是否需要读取对应工位的数据
                OperateResult<int> ret = cip.ReadInt32("Auto_process[18]");
                if (ret.IsSuccess)
                {
                    if (ret.Content < 50 && ret.Content >= 25)  ReadStation(dingfeng, cip);

                }
                else
                {
                    logNet.WriteInfo("Auto_process[18] read failed ");
                    //Console.WriteLine("Auto_process[18] read failed");
                }

                ret = cip.ReadInt32("Auto_process[9]");
                if (ret.IsSuccess)
                {
                    if (ret.Content >= 40 && ret.Content < 70)  ReadStation(chongmo, cip);
                }
                else
                {
                    logNet.WriteInfo("Auto_process[9] read failed ");
                    //Console.WriteLine("Auto_process[9] read failed");
                }

                ret = cip.ReadInt32("Auto_process[43]");
                if (ret.IsSuccess)
                {
                    if (ret.Content < 45 && ret.Content >= 30) ReadStation(zuojiaofeng, cip);
                }
                else
                {
                    logNet.WriteInfo("Auto_process[43] read failed ");
                    //Console.WriteLine("Auto_process[43] read failed");
                }

                TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
                DateTime nowDisplay = DateTime.Now;
                TimeSpan dur = (start - end).Duration();

                //logNet.WriteInfo("Thread ReadStationInfo2 read time : " + (dur.TotalMilliseconds).ToString());
                Console.WriteLine("Thread ReadStationInfo2 read time:{0} read Duration:{1}", nowDisplay.ToString("yyyy-MM-dd HH:mm:ss:fff"), dur.TotalMilliseconds);


                if (dur.TotalMilliseconds < 100)
                {
                    int sleepTime = 100 - (int)dur.TotalMilliseconds;
                    Thread.Sleep(sleepTime);
                }
            }

        }
        #endregion

        public void ReadStationInfo3(OmronConnectedCipNet cip)
        {
            while (true)
            {
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);

                //根据Auto_Process的值，判断是否需要读取对应工位的数据


                OperateResult<int> ret = cip.ReadInt32("Auto_process[44]");
                if (ret.IsSuccess)
                {
                    if (ret.Content < 45 && ret.Content >= 30) ReadStation(youjiaofeng, cip);  
                }
                else
                {
                    logNet.WriteInfo("Auto_process[44] read failed ");
                    Console.WriteLine("Auto_process[44] read failed");
                }


                ret = cip.ReadInt32("Auto_process[47]");
                if (ret.IsSuccess)
                {
                    if (ret.Content < 60 && ret.Content >= 30) ReadStation(cefeng, cip);
                }
                else
                {
                    logNet.WriteInfo("Auto_process[47] read failed ");
                    Console.WriteLine("Auto_process[47] read failed");
                }

                TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
                DateTime nowDisplay = DateTime.Now;
                TimeSpan dur = (start - end).Duration();

                logNet.WriteInfo("Thread ReadStationInfo3 read time : " + (dur.TotalMilliseconds).ToString());
                Console.WriteLine("Thread ReadStationInfo3 read time:{0} read Duration:{1}", nowDisplay.ToString("yyyy-MM-dd HH:mm:ss:fff"), dur.TotalMilliseconds);


                if (dur.TotalMilliseconds < 100)
                {
                    int sleepTime = 100 - (int)dur.TotalMilliseconds;
                    Thread.Sleep(sleepTime);
                }
            }

        }


        #region Function 读取六个工位的数据
        public  async void ReadStation(StationInfoStruct_CIP[] input, OmronConnectedCipNet cip)
        {
            var tempstring = "";  //暂存取到的string数据
            int count = 0; //计数器
            string StationName_Now = CN2EN(input[0].stationName); //将当前结构体数组的工位名读取出来，后续在xml文件中对应,中文转拼音（英文）
            var listWriteItem = new List<WriteItem>();
            WriteItem[] writeItems = new WriteItem[] { };
  
            foreach (var tmp in input)
            {
              
                if (tmp.varType == "DINT")
                {

                    OperateResult<int> ret =  cip.ReadInt32(tmp.varName);

                    if (ret.IsSuccess)
                    {
                        tempstring += ret.Content.ToString() + ",";
                        count++;
                    }
                    else
                    {
                        //logNet.WriteInfo(tmp.varName + "read failed");
                        Console.WriteLine(tmp.varName + "read failed");

                    }
                }

                if (tmp.varType == "INT")
                {

                    OperateResult<short> ret =  cip.ReadInt16(tmp.varName);
                    if (ret.IsSuccess)
                    {
                        tempstring += ret.Content.ToString() + ",";
                        count++;
                    }
                    else
                    {
                        //logNet.WriteInfo(tmp.varName + "read failed");
                        Console.WriteLine(tmp.varName + "read failed");

                    }
                }

                if (tmp.varType == "REAL")
                {

                    OperateResult<float> ret =  cip.ReadFloat(tmp.varName);
                    if (ret.IsSuccess)
                    {
                        tempstring += ret.Content.ToString() + ",";
                        count++;
                    }
                    else
                    {
                        //logNet.WriteInfo(tmp.varName + "read failed");
                        Console.WriteLine(tmp.varName + "read failed");
                    }
                }


                if (count == input.Length)
                {
                    #region Grpc发送给IEC

                    writeItems = null;
                    try
                    {
                        listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary.GetValueOrDefault(StationName_Now), Arp.Type.Grpc.CoreType.CtString, tempstring)); //todo:待优化floatArr改为Content
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERRO: {0}", e);
                    }

                    SendDataToIEC(listWriteItem);
                    #endregion
                }
            }
        }
        #endregion  Function 读取六个工位的数据


        #region Function 读取Auto_Process 和Clear_Manual （以数组形式一起读上来，再按照序号写入对应的工位里）
        public void ReadDeviceInfoConSturct1(DeviceInfoConSturct1_CIP[] input, OmronConnectedCipNet cip , string[] Output)
        {
            string ReadObject = input[0].varName;   //！这里约定变量名就叫Auto_process 索引单独是一个变量！
            ushort Auto_Process_Length = 86;  // 数组长度为硬编码，由Excel读出，不知后续需要是否需要更改
            ushort Clear_Manual = 75;
            var tempstring = ""; //用来发送的字符串

            if (input[0].varType == "DINT")
            {
                OperateResult<int[]> ret = cip.ReadInt32(ReadObject, Auto_Process_Length);
                if (ret.IsSuccess)
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        tempstring = ret.Content[input[i].varIndex].ToString()+ ",";  
                        Output[input[i].stationNumber] += tempstring;
                    }

                }
                else
                {
                    logNet.WriteInfo(ReadObject + "read failed");
                    //Console.WriteLine(ReadObject + "read failed");

                }
            }
            if (input[0].varType == "BOOL")
            {
                OperateResult<bool[]> ret = cip.ReadBool(ReadObject, Clear_Manual);
                if (ret.IsSuccess)
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        tempstring = ret.Content[input[i].varIndex] ? "1," : "0," ;                                                
                        Output[input[i].stationNumber] += tempstring;
                    }
                }
                else
                {
                    logNet.WriteInfo(ReadObject + "read failed");
                    //Console.WriteLine(ReadObject + "read failed");

                }
            }
        }
        #endregion Function 读取Auto_Process 和Clear_Manual （以数组形式一起读上来，再按照序号写入对应的工位里）


        #region Function 读取剩余的设备信息（每个数据都是一个个读取）
        public void ReadDeviceInfoDisStruct2(DeviceInfoDisStruct2_CIP[] input, OmronConnectedCipNet cip, string[] Output)
        {

            var tempstring = ""; //用来发送的字符串
            ushort RealVariableNumber = 50;  //电芯条码地址和极耳码地址写死了
            if (input[0].varType == "BOOL")  //这里传入参数input的type统一 ，整个结构体数组要么都是BOOL类型，要么都是REAL类型，所以根据第一个元素的Type进行数据类型判断
            {
                foreach(var tmp in input)
                {
                    OperateResult<bool> ret = cip.ReadBool(tmp.varName);
                    if (ret.IsSuccess)
                    {
                        tempstring = ret.Content ? "1," : "0," ;
                        Output[tmp.stationNumber] += tempstring;
                    }
                    else
                    {
                        logNet.WriteInfo(tmp.varName + "read failed");
                        //Console.WriteLine(tmp.varName + "read failed");

                    }

                }

            }
            if (input[0].varType == "REAL")
            {
                foreach (var tmp in input)
                {
                    OperateResult<float[]> ret = cip.ReadFloat(tmp.varName, RealVariableNumber);
                    if (ret.IsSuccess)
                    {
                        tempstring = "";
                        foreach (var f in ret.Content)
                        {
                            tempstring = tempstring + f.ToString();
                        }

                        /* TO DO 转ASCII码 */
                         
                        Output[tmp.stationNumber] += tempstring;
                        
                    }
                    else
                    {
                        logNet.WriteInfo(tmp.varName + "read failed");
                        //Console.WriteLine(tmp.varName + "read failed");

                    }

                }
            }
        }
        #endregion Function 读取剩余的设备信息（每个数据都是一个个读取）


        #region Function 读取1000ms的数据 （功能开关，生产统计，报警信号，寿命管理)
        public void ReadOneSecData(OneSecInfoStruct_CIP[] input, OmronConnectedCipNet cip)
        {
            ushort ush;
            var tempstring = "";  //暂存取到的string数据
            int StructNumber = 0; //记录中文结构体读了多少个了，读完以后一起发给Grpc
            var listWriteItem = new List<WriteItem>();
            WriteItem[] writeItems = new WriteItem[] { };

            if (input[0].varType == "BOOL" && input[0].varName != "Manual_Andon[10]")   //区分Manual_Andon 不连续数组
            {
                ush = (ushort)input.Length;
                OperateResult<bool[]> ret = cip.ReadBool(input[0].varName, ush);

                if (ret.IsSuccess)
                {
                    #region Grpc发送给IEC

                    writeItems = null;  //先清空
                    try
                    {
                        listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary.GetValueOrDefault(input[0].varName), Arp.Type.Grpc.CoreType.CtArray, ret.Content));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERRO: {0}", e);
                    }

                   SendDataToIEC(listWriteItem);  //不等待其完成

                    #endregion 

                }
                else
                {
                    //logNet.WriteInfo(input[0].varName + "read failed");
                    Console.WriteLine(input[0].varName + "read failed");
                }
            }

            if (input[0].varType == "BOOLStruct" || input[0].varName == "Manual_Andon[10]")
            {

                foreach(var tmp in input)
                {
                    OperateResult<bool> ret = cip.ReadBool(tmp.varName); //try 
 
                    if (ret.IsSuccess)
                    {
                        tempstring += ret.Content ? "1," : "0,";
                        StructNumber++;
                        if (StructNumber == input.Length)
                        {
                            StructNumber = 0; //计数清零

                            #region Grpc发送给IEC
                            writeItems = null;

                            listWriteItem.Clear();

                            try
                            {
                                if (input[0].varType == "BOOLStruct")
                                {
                                    listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary.GetValueOrDefault("out_power"), Arp.Type.Grpc.CoreType.CtString, tempstring));
                                }
                                if(input[0].varName == "Manual_Andon[10]")
                                {
                                    listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary.GetValueOrDefault("Manual_Andon[10]"), Arp.Type.Grpc.CoreType.CtString, tempstring));

                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("ERRO: {0}", e);
                            }

                            SendDataToIEC(listWriteItem);  //不等待其完成

                            #endregion 

                        }
                    }

                    else
                    {
                        //logNet.WriteInfo(tmp.varName + "read failed");
                        Console.WriteLine(tmp.varName + "read failed");
                    }

                }       
            }

            if (input[0].varType == "DINT")
            {
                ush = (ushort)input.Length;
                OperateResult<int[]> ret = cip.ReadInt32(input[0].varName, ush);
                if (ret.IsSuccess)
                {

                    #region Grpc发送给IEC

                    writeItems = null;
                    try
                    {
                        listWriteItem.Add(grpcToolInstance.CreatWriteItem(nodeidDictionary.GetValueOrDefault(input[0].varName), Arp.Type.Grpc.CoreType.CtArray, ret.Content));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERRO: {0}", e);
                    }

                    SendDataToIEC(listWriteItem);  
                    #endregion 

                }
                else
                {
                    //logNet.WriteInfo(input[0].varName + "read failed");
                    Console.WriteLine(input[0].varName + "read failed");
                }
            }

        }
        #endregion


        private void SendDataToIEC(List<WriteItem> writeItems)
        {
            try
            {
                var writeItemsArray = writeItems.ToArray();
                var dataAccessServiceWriteRequest = grpcToolInstance.ServiceWriteRequestAddDatas(writeItemsArray);
                bool result = grpcToolInstance.WriteDataToDataAccessService(grpcDataAccessServiceClient, dataAccessServiceWriteRequest, new IDataAccessServiceWriteResponse(), options1);

            }
            catch(Exception e) 
            {
                Console.WriteLine("ERRO: {0}", e);
            }


        }

        //XML标签转换 工位结构体数组的工位名是中文，为了方便XML与字典对应，需要转化为英文
        public string CN2EN(string NameCN)
        {
            string NameEN = "";

            switch(NameCN)
            {
                case "加工工位（冲膜）":
                    NameEN = "chongmo";
                    break;

                case "加工工位（热压）":
                    NameEN = "reya";
                    break;

                case "加工工位（顶封）":
                    NameEN = "dingfeng";
                    break;

                case "加工工位（右角封）":
                    NameEN = "youjiaofeng";
                    break;

                case "加工工位（左角封）":
                    NameEN = "zuojiaofeng";
                    break;

                case "加工工位（侧封）":
                    NameEN = "cefeng";
                    break;

                default:
                    break;

            }

            return NameEN;

        }

    }
}

