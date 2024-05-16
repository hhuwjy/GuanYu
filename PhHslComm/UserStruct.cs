using Arp.Type.Grpc;
using Google.Protobuf.WellKnownTypes;
using HslCommunication;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PhHslComm
{
    public class UserStruct
    {

        #region getTypeStruct

        public TypeStruct getTypeStruct(object stru)
        {
            TypeStruct structV = new TypeStruct();
            if (stru.GetType() == typeof(zuojiaofeng))
            {
                zuojiaofeng StructValue = (zuojiaofeng)stru;
                structV = new UserStruct().getTypeStruct_zuojiaofeng(StructValue);
            }
            if (stru.GetType() == typeof(UDT_StationInfo))
            {
                UDT_StationInfo StructValue = (UDT_StationInfo)stru;
                structV = new UserStruct().getTypeStruct_UDT_StationInfo(StructValue);
            }
            if (stru.GetType() == typeof(testStruct))
            {
                testStruct StructValue = (testStruct)stru;
                structV = new UserStruct().getTypeStruct_testStruct(StructValue);
            }
            return structV;

        }

        #endregion 


        #region getTypeArrray
        public TypeArray getTypeArrray(object Arr)
        {
            TypeArray ArrayV = new TypeArray();

            if (Arr.GetType() == typeof(float[]))
            {
                float[] floatArr = (float[])Arr;

                foreach (float f in floatArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.FloatValue = f;
                    objectType.TypeCode = CoreType.CtReal32;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }

            if (Arr.GetType() == typeof(Int32[]))
            {
                Int32[] dintArr = (Int32[])Arr;

                foreach (Int32 f in dintArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.Int32Value = f;
                    objectType.TypeCode = CoreType.CtInt32;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }

            if (Arr.GetType() == typeof(string[]))
            {
                string[] stringArr = (string[])Arr;

                foreach (string s in stringArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.StringValue = s.PadRight(85, ' ');
                    objectType.TypeCode = CoreType.CtString;
                    ArrayV.ArrayElements.Add(objectType);
                }
            }

            if (Arr.GetType() == typeof(bool[]))
            {
                bool[] boolArr = (bool[])Arr;

                foreach (bool f in boolArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.BoolValue = f;
                    objectType.TypeCode = CoreType.CtBoolean;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }


            if (Arr.GetType() == typeof(UDT_StationInfo[]))
            {
                UDT_StationInfo[] testStructArr = (UDT_StationInfo[])Arr;

                foreach (UDT_StationInfo f in testStructArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.StructValue = getTypeStruct_UDT_StationInfo(f);
                    objectType.TypeCode = CoreType.CtStruct;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }


            if (Arr.GetType() == typeof(testStruct[]))
            {
                testStruct[] testStructArr = (testStruct[])Arr;

                foreach (testStruct f in testStructArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.StructValue = getTypeStruct_testStruct(f);
                    objectType.TypeCode = CoreType.CtStruct;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }


            if (Arr.GetType() == typeof(stringStruct[]))
            {
                stringStruct[] testStructArr = (stringStruct[])Arr;

                foreach (stringStruct f in testStructArr)
                {
                    ObjectType objectType = new ObjectType();
                    objectType.StructValue = getTypeStruct_stringStruct(f);
                    objectType.TypeCode = CoreType.CtStruct;
                    ArrayV.ArrayElements.Add(objectType);
                }

            }

            return ArrayV;

        }
        #endregion




        #region stringStruct
        public struct stringStruct
        {
            public string str;            
        }
        public TypeStruct getTypeStruct_stringStruct(stringStruct StructValue)
        {
            TypeStruct structV = new TypeStruct();

            ObjectType v0 = new ObjectType();
            v0.StringValue = StructValue.str;
            v0.TypeCode = CoreType.CtString;
            structV.StructElements.Add(v0);

            

            return structV;
        }
        #endregion

        #region zuojiaofeng
        public struct zuojiaofeng
        {
            public short qdlW2RS;
            public short qdlW2RT;
            public short qdlW3RS;
            public short qdlW3RT;
            public int rp841;
            public int bili;
            public float zhengya;
            public int rp1942;
        }
        public TypeStruct getTypeStruct_zuojiaofeng(zuojiaofeng StructValue)
        {           
            TypeStruct structV = new TypeStruct();

            ObjectType v0 = new ObjectType();
            v0.Int16Value = StructValue.qdlW2RS;
            v0.TypeCode = CoreType.CtInt16;
            structV.StructElements.Add(v0);

            ObjectType v1 = new ObjectType();
            v1.Int16Value = StructValue.qdlW2RT;
            v1.TypeCode = CoreType.CtInt16;
            structV.StructElements.Add(v1);

            ObjectType v2 = new ObjectType();
            v2.Int16Value = StructValue.qdlW3RS;
            v2.TypeCode = CoreType.CtInt16;
            structV.StructElements.Add(v2);

            ObjectType v3 = new ObjectType();
            v3.Int16Value = StructValue.qdlW2RS;
            v3.TypeCode = CoreType.CtInt16;
            structV.StructElements.Add(v3);

            ObjectType v4 = new ObjectType();
            v4.Int32Value = StructValue.rp841;
            v4.TypeCode = CoreType.CtInt32;
            structV.StructElements.Add(v4);

            ObjectType v5 = new ObjectType();
            v5.FloatValue = StructValue.bili;
            v5.TypeCode = CoreType.CtInt32;
            structV.StructElements.Add(v5);

            ObjectType v6 = new ObjectType();
            v6.FloatValue = StructValue.zhengya;
            v6.TypeCode = CoreType.CtReal32;
            structV.StructElements.Add(v6);

            ObjectType v7 = new ObjectType();
            v7.Int32Value = StructValue.rp1942;
            v7.TypeCode = CoreType.CtInt32;
            structV.StructElements.Add(v7);

            return structV;
        }
        #endregion


        #region UDT_StationInfo
        //每个工位的信息
        public struct UDT_StationInfo
        {
            public bool xCellMem; //电芯记忆信号（当前工位有电芯，用于组合电芯条码做条码转移和参数绑定）
            public bool  xCellMemClear; //电芯记忆清除信号
            public bool xStationBusy; //工位加工中信号（用于触发100ms参数采集，短于）
            public string strCellCode; //电芯条码
            public string strPoleEarCode; //极耳码
            public bool xIsProcessStation; //是否加工工位  
        }

        public TypeStruct getTypeStruct_UDT_StationInfo(UDT_StationInfo StructValue)
        {
            TypeStruct structV = new TypeStruct();

            ObjectType v0 = new ObjectType();
            v0.BoolValue = StructValue.xCellMem;
            v0.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v0);

            ObjectType v1 = new ObjectType();
            v1.BoolValue = StructValue.xCellMemClear;
            v1.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v1);

            ObjectType v2 = new ObjectType();
            v2.BoolValue = StructValue.xStationBusy;
            v2.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v2);

            ObjectType v3 = new ObjectType();
            v3.StringValue = StructValue.strCellCode;
            v3.TypeCode = CoreType.CtString;
            structV.StructElements.Add(v3);

            ObjectType v4 = new ObjectType();
            v4.StringValue = StructValue.strPoleEarCode;
            v4.TypeCode = CoreType.CtString;
            structV.StructElements.Add(v4);

            ObjectType v5 = new ObjectType();
            v5.BoolValue = StructValue.xIsProcessStation;
            v5.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v5);

            return structV;
        }
        #endregion


        #region testStruct
        public struct testStruct
        {           
            public bool xCellMem; //电芯记忆信号（当前工位有电芯，用于组合电芯条码做条码转移和参数绑定）
            public bool xCellMemClear; //电芯记忆清除信号
            public bool xStationBusy; //工位加工中信号（用于触发100ms参数采集，短于）
            public string strCellCode; //电芯条码
            public string strPoleEarCode; //极耳码
            public bool xIsProcessStation; //是否加工工位  
            public zuojiaofeng zjf;
            public float[] floatArr;
        }

        public TypeStruct getTypeStruct_testStruct(testStruct StructValue)
        {
            TypeStruct structV = new TypeStruct();

            ObjectType v0 = new ObjectType();
            v0.BoolValue = StructValue.xCellMem;
            v0.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v0);

            ObjectType v1 = new ObjectType();
            v1.BoolValue = StructValue.xCellMemClear;
            v1.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v1);

            ObjectType v2 = new ObjectType();
            v2.BoolValue = StructValue.xStationBusy;
            v2.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v2);

            ObjectType v3 = new ObjectType();
            v3.StringValue = StructValue.strCellCode;
            v3.TypeCode = CoreType.CtString;
            structV.StructElements.Add(v3);

            ObjectType v4 = new ObjectType();
            v4.StringValue = StructValue.strPoleEarCode;
            v4.TypeCode = CoreType.CtString;
            structV.StructElements.Add(v4);

            ObjectType v5 = new ObjectType();
            v5.BoolValue = StructValue.xIsProcessStation;
            v5.TypeCode = CoreType.CtBoolean;
            structV.StructElements.Add(v5);

            ObjectType v6 = new ObjectType();
            v6.StructValue = getTypeStruct_zuojiaofeng(StructValue.zjf);
            v6.TypeCode = CoreType.CtStruct;
            structV.StructElements.Add(v6);

            ObjectType v7 = new ObjectType();
            v7.ArrayValue = getTypeArrray(StructValue.floatArr);
            v7.TypeCode = CoreType.CtArray;
            structV.StructElements.Add(v7);

            return structV;
        }

        #endregion


        


    }
}
