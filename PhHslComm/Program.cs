// See https://aka.ms/new-console-template for more information


using HslCommunication;
using HslCommunication.LogNet;
using HslCommunication.Profinet.Omron;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Net;
using Newtonsoft.Json.Linq;
using HslCommunication.MQTT;
using HslCommunication.Profinet.Siemens;
using HslCommunication.Profinet.OpenProtocol;
using NPOI.XSSF.UserModel;
using static PhHslComm.UserStruct;


namespace PhHslComm
{ 
    class Program
    {
        static void Main(string[] args)
        {
            int clientNum = 5; //最大32

            #region 将Excel数据读取到readExcel变量中
           // string excelFilePath = Directory.GetCurrentDirectory() + "\\HGFZData.xlsx";

            string excelFilePath = "/opt/plcnext/apps/HGFZData.xlsx";

            ReadExcel readExcel = new ReadExcel();
            XSSFWorkbook excelWorkbook = readExcel.connectExcel(excelFilePath);
            if (excelWorkbook != null)
            {
                Console.WriteLine("excelWorkbook reasd success");
            }
            else
            {
                Console.WriteLine("excelWorkbook reasd fail");
            }
            #endregion


            OmronComm omronClients = new OmronComm();
   

            #region 将子表内容写入对应结构体数组中

            // 六大工位（100ms)
            omronClients.chongmo = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（冲膜）");
            omronClients.reya = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（热压）");
            omronClients.dingfeng = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（顶封）");
            omronClients.zuojiaofeng = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（左角封）");
            omronClients.youjiaofeng = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（右角封）");
            omronClients.cefeng = readExcel.ReadStationInfo_Excel(excelWorkbook, "加工工位（侧封）");

            // 非报警信号（1000ms）
            omronClients.Sys_Manual = readExcel.ReadOneSecInfo_Excel(excelWorkbook,"功能开关");
            omronClients.Production_statistics = readExcel.ReadOneSecInfo_Excel(excelWorkbook,"生产统计");
            omronClients.Cutterused_statistics = readExcel.ReadOneSecInfo_Excel(excelWorkbook,"寿命管理");
            omronClients.Y6 = readExcel.ReadOneSecInfo_Excel(excelWorkbook,"OEE");
            omronClients.Manual_Andon = readExcel.ReadOneSecInfo_Excel(excelWorkbook,"OEE(2)");
            if (omronClients.Manual_Andon!=null)
            {
                Console.WriteLine("OEE(2) reasd success");
            }
            else {
                Console.WriteLine("OEE(2) reasd fail");
            }

            // 报警信号（1000ms)
            omronClients.Vacuum_Alarm = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Vacuum_Alarm");
            omronClients.Senor_Alarm = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Senor_Alarm");
            omronClients.Motor_POTLimit_Err = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Motor_POTLimit_Err");
            omronClients.Motor_NOTLimit_Err = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Motor_NOTLimit_Err");
            omronClients.Motor_Prevent_Promt = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Motor_Prevent_Promt");
            omronClients.Exception_information = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Exception_information");
            omronClients.Operation_prompt = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Operation_prompt");
            omronClients.Temperature_Alarm = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Temperature_Alarm");
            omronClients.Cylinder_Reset_Promt = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Cylinder_Reset_Promt");
            omronClients.Motor_Reset_Promt = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Motor_Reset_Promt");
            omronClients.Stopstate_Error = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Stopstate_Error");
            omronClients.Grating_Error = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Grating_Error");
            omronClients.Scapegoat_Error = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Scapegoat_Error");
            omronClients.Door_Error = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "Door_Error");
            omronClients.out_power = readExcel.ReadOneSecInfo_Excel(excelWorkbook, "out_power");

            // 设备信息（100ms）
            omronClients.Auto_Process = readExcel.ReadOneDeviceInfoConSturct1Info_Excel(excelWorkbook,"设备信息",3);
            omronClients.Clear_Manual = readExcel.ReadOneDeviceInfoConSturct1Info_Excel(excelWorkbook,"设备信息",5);
            omronClients.Battery_Memory = readExcel.ReadOneDeviceInfoDisSturct2Info_Excel(excelWorkbook, "设备信息",4);
            omronClients.BarCode = readExcel.ReadOneDeviceInfoDisSturct2Info_Excel(excelWorkbook, "设备信息", 6);
            omronClients.EarCode = readExcel.ReadOneDeviceInfoDisSturct2Info_Excel(excelWorkbook, "设备信息", 7);

            #endregion

            omronClients.connect(); //连接CIP和Grpc
            omronClients.startThr(); //开始读写数据
           
            Console.ReadKey();



        }

    }
}



 

