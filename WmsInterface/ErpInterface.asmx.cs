//============================================================================
// Webservice接口程序:
//----------------------------------------------------------------------------
// 描述: 
//----------------------------------------------------------------------------
// 参数:(无)
//----------------------------------------------------------------------------
// 返回值:  (none)
//----------------------------------------------------------------------------
// 作者:	lwb		日期: 2016.11.08
//----------------------------------------------------------------------------
// 修改历史: 
//	
//============================================================================
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using Oracle.ManagedDataAccess.Client;
namespace ErpInterface
{
    #region 初始设置
    [WebService(Namespace = "mia.hn.cn")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    /*
     * 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
     * [System.Web.Script.Services.ScriptService]
     */
    #endregion
    public class ErpInterface : System.Web.Services.WebService
    {
        #region 业务变量定义
        public string address = "";
        public string ib_iflabel = "";
        public int host;
        public int webservicehost;
        public int timers;
        public TcpClient _client = null;
        public MemoryStream memory = new MemoryStream();
        public XmlDocument xmlDoc = new XmlDocument();
        public XmlDocument doc = new XmlDocument();
        public Oracle.ManagedDataAccess.Client.OracleTransaction trans;/*事务处理类*/
        Oracle.ManagedDataAccess.Client.OracleCommand cmd = null;
        public bool inTransaction = false;/*指示当前是否正处于事务中*/
        public Oracle.ManagedDataAccess.Client.OracleConnection cn; /*数据库连接*/
        private Int32 returncode = -1;//存储过程返回编码
        private string returnmsg = "";//存储过程返回信息
        #endregion

        #region 类变量定义
        /*
		 * / <summary>
		 * / 操作时间
		 * / </summary>
		 */
        private string Opertime = "";

        /*
         * / <summary>
         * / 通讯密匙
         * / </summary>
         */
        private string Interface = "";

        /*
         * / <summary>
         * / 服务器名
         * / </summary>
         */
        private string DBServer = "";

        /*
         * / <summary>
         * / 服务器名
         * / </summary>
         */
        private string Port = "";

        /*
         * / <summary>
         * / 服务器名
         * / </summary>
         */
        private string Host = "";

        /*
         * / <summary>
         * / 服务器名
         * / </summary>
         */
        private string Server_Name = "";

        /*
         * / <summary>
         * / 数据库连接用户ID
         * / </summary>
         */
        private string UserID = "";

        /*
         * / <summary>
         * / 数据库连接用户密码
         * / </summary>
         */
        private string PassWord = "";

        /*
         * / <summary>
         * / 操作用户ID
         * / </summary>
         */
        private string OperUserID = "";

        /*
         * / <summary>
         * / 操作用户密码
         * / </summary>
         */
        private string OperPassWord = "";

        /*
         * / <summary>
         * / 接口连接用户ID
         * / </summary>
         */
        private string InterfaceUserID = "";

        /*
         * / <summary>
         * / 接口连接用户密码
         * / </summary>
         */
        private string InterfacePassWord = "";

        /*
         * / <summary>
         * / 数据库联接串
         * / </summary>
         */
        private string DBConnStr = "";

        /*
         * / <summary>
         * / 当前用户是否有效
         * / </summary>
         */
        private bool IsValidUser = false;

        /*
         * / <summary>
         * / 当前用户所在的机构代码
         * / </summary>
         */
        private string OrgCode = "";

        /*
         * / <summary>
         * / 功能编码
         * / </summary>
         */
        private string FunctionId = "";

        /*
         * / <summary>
         * / 当前用户所在的机构名称
         * / </summary>
         */
        private string OrgName = "";

        /*
         * / <summary>
         * / 当前DB处理类型
         * / </summary>
         */
        private string controlip = "";

        /*
         * / <summary>
         * / 接口拥有的权限
         * / </summary>
         * private int InterFacePower = -1;
         */
        #endregion

        #region 数据库连接相关
        /*
		 * / <summary>
		 * / 获取数据库联接参数串
		 * / </summary>
		 * / <param name="OrgCode"></param>
		 * / <returns></returns>
		 */
        public bool getcnParms(string databaseini, out string mess)
        {
            XmlTextReader txtReader = new XmlTextReader(Server.MapPath("./DBConn/" + databaseini + ".xml"));
            try
            {
                /* 找到符合的节点获取需要的属性值 */
                while (txtReader.Read())
                {
                    txtReader.MoveToElement();
                    if (txtReader.Name == "org")
                    {
                        if (txtReader.GetAttribute("code") == OrgCode)
                        {
                            DBServer = txtReader.GetAttribute("DBServer");
                            Port = txtReader.GetAttribute("PORT");
                            Host = txtReader.GetAttribute("HOST");
                            Server_Name = txtReader.GetAttribute("SERVICE_NAME");
                            UserID = txtReader.GetAttribute("UserID");
                            PassWord = txtReader.GetAttribute("PassWord");
                            break;
                        }
                        if (txtReader.NodeType.ToString() == "EndElement")
                        {
                            break;
                        }
                    }
                }
                if (DBServer == "")
                {
                    mess = "获取机构" + OrgCode + "的数据库联接参数错误，请检查配置文件！";
                    RollbackTrans();
                    return (false);
                }
                else
                {
                    mess = "Data Source=(DESCRIPTION =    (ADDRESS_LIST =      (ADDRESS = (PROTOCOL = TCP)(HOST = " + Host + ")(PORT = " + Port + "))    )    (CONNECT_DATA =      (SERVER = DEDICATED)      (SERVICE_NAME = " + Server_Name + ")    )  );Persist Security Info=True;User ID=" + UserID + ";Password=" + PassWord + ";";
                    return (true);
                }
            }
            catch (Exception e)
            {
                if (e.Message.ToString().Contains("未能找到文件"))
                {
                    mess = "服务器没有找到机构" + OrgCode + "的数据库联接配置参数！";
                }
                else
                {
                    mess = "获取机构" + OrgCode + "的数据库联接参数错误：" + e.Message.ToString();
                }
                RollbackTrans();
                return (false);
            }
            finally
            {
                txtReader.Close();
            }
        }

        /*
         * / <summary>
         * / 根据给定的参数类型获得数据库联接串
         * / </summary>
         * / <param name="DataType"></param>
         * / <param name="Data"></param>
         * / <param name="mess"></param>
         * / <returns></returns>
         */
        public bool getDBConnStr(string DataType, string Data, out string mess)
        {
            try
            {
                if (DBConnStr != "")
                {
                    mess = DBConnStr;
                    return (true);
                }
                else
                {
                    switch (DataType)
                    {
                        case "OrgCode":
                            OrgCode = Data;
                            break;
                        case "hospitalcode":
                            OrgCode = Data.Substring(0, 6);
                            break;
                        case "bookcard":
                            OrgCode = Data.Substring(0, 6);
                            break;
                        default:
                            mess = "指定的区域型数据类型错误，无法去联接数据库！";
                            return (false);
                    }
                    if (getcnParms(OrgCode, out mess))
                    {
                        DBConnStr = "Provider=SQLOLEDB.1;Persist Security Info=False;" + mess;
                        return (true);
                    }
                    else
                    {
                        return (false);
                    }
                }
            }
            catch (Exception e)
            {
                DBConnStr = "";
                mess = "寻址数据库异常：" + e.Message.ToString();
                return (false);
            }
        }
        #endregion

        #region 用户状态
        /*
		 * / <summary>
		 * / 得到当前用户是否有效
		 * / </summary>
		 * / <returns></returns>
		 */
        public bool getUserState()
        {
            if (IsValidUser)
            {
                return (true);
            }
            return (false);
        }

        /*
         * / <summary>
         * / 设置当前用户是否有效
         * / </summary>
         * / <param name="IfValid"></param>
         * / <returns></returns>
         */
        public bool setUserState(bool IfValid)
        {
            IsValidUser = IfValid;
            return (IfValid);
        }
        #endregion

        #region 用户验证
        /*
		 * / <summary>
		 * / 验证用户身份，返回“TRUE”表示验证通过，InterFacePower表示允许使用的接口业务类型
		 * / </summary>
		 * / <param name="areacode"></param>
		 * / <param name="hospitalcode"></param>
		 * / <param name="userid"></param>
		 * / <param name="pwd"></param>
		 * / <returns></returns>
		 * [WebMethod(Description="Service用户身份验证，返回“TRUE”表示验证通过")]
		 */
        public string checkUserValid(string FunctionId, string orgcode, string userid, string pwd, string operuserid, string operuserpass, string mess, int type, int checkoperuer, out string ls_xml)
        {
            cn = new Oracle.ManagedDataAccess.Client.OracleConnection(mess);
            cmd = null;
            OracleDataReader myReader = null;
            ls_xml = null;
            try
            {
                Open();
                BeginTrans();
                if (type == 0)
                {
                    return ("TRUE");
                }
                if (type == 1)
                {
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand("SELECT StoreName FROM Tb_Wms_Store WHERE StoreCode='" + orgcode + "' AND pdaUser=('" + userid + "') AND pdaPass=('" + pwd + "')", cn);
                }
                if (inTransaction)
                {
                    cmd.Transaction = trans;
                }
                myReader = cmd.ExecuteReader();
                if (!myReader.HasRows)
                {
                    return ("ERP库代码或接口用户代码或密码错误，身份验证失败！");
                }
                else
                {
                    myReader.Read();
                    OrgName = myReader.GetString(0);
                    if (checkoperuer == 1) /* 需要用户身份验证 */
                    {
                        cmd = new Oracle.ManagedDataAccess.Client.OracleCommand("SELECT UserName FROM Tb_Shop_PostPerson WHERE  UserCode=('" + operuserid + "') AND UserPass=('" + operuserpass + "') AND IfPDA = '1'", cn);
                        if (inTransaction)
                        {
                            cmd.Transaction = trans;
                        }
                        myReader = cmd.ExecuteReader();
                        if (!myReader.HasRows)
                        {
                            return ("操作用户代码或密码错误，身份验证失败！");
                        }
                        else
                        {
                            setUserState(true);
                            return ("TRUE");
                        }
                    }
                    else
                    { /*不需要用户身份验证 */
                        setUserState(true);
                        return ("TRUE");
                    }
                }
            }
            catch (Exception e)
            {
                RollbackTrans();
                return ("验证异常！" + e.Message.ToString());
            }
            finally
            {

                if (myReader != null)
                {
                    if (!myReader.IsClosed)
                        myReader.Close();
                    myReader.Dispose();
                }
                cmd = null;
            }
        }
        #endregion

        #region 获取GUID
        public string getguid()
        {
            string ls_return = "";
            Oracle.ManagedDataAccess.Client.OracleCommand cmdnew;
            try
            {
                Open();
                cmdnew = new Oracle.ManagedDataAccess.Client.OracleCommand("SELECT createguid() from dual", cn);
                if (inTransaction)
                {
                    cmdnew.Transaction = trans;
                }
                OracleDataReader myReader = cmdnew.ExecuteReader();
                try
                {
                    if (!myReader.HasRows)
                    {
                        return ("获取GUID失败！");
                    }
                    else
                    {
                        myReader.Read();
                        ls_return = myReader.GetString(0);
                        return (ls_return);
                    }
                }
                finally
                {
                    if (myReader != null)
                    {
                        if (!myReader.IsClosed)
                            myReader.Close();
                        myReader.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                return ("获取GUID异常！" + e.Message.ToString());
            }
            finally
            {
                cmdnew = null;
            }
        }
        #endregion

        #region 封装函数
        //连接服务器
        public void ConnServer()
        {
            try
            {
                _client = new TcpClient(address, host);
            }
            //处理参数为空引用异常 
            catch (ArgumentNullException ae)
            {
                Console.WriteLine("ArgumentNullException : {0}", ae.Message.ToString());
                throw new ArgumentNullException("参数异常" + ae.Message.ToString());
            }
            //处理操作系统异常 
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.Message.ToString());
                throw new Exception("连接异常：" + se.Message.ToString());
            }
            catch (Exception ew)
            {
                Console.WriteLine("Unexpected exception : {0}", ew.Message.ToString());
                throw new Exception("其它异常：" + ew.Message.ToString());
            }
        }

        public byte[] StringToByte(string InString)
        {
            string[] ByteStrings;
            ByteStrings = InString.Split(" ".ToCharArray());
            byte[] ByteOut;
            ByteOut = new byte[ByteStrings.Length - 1];
            for (int i = 0; i == ByteStrings.Length - 1; i++)
            {
                ByteOut[i] = Convert.ToByte(("0x" + ByteStrings[i]));
            }
            return ByteOut;
        }

        public byte[] strToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        public string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }

        #region crc
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        public long GetModBusCRC(string DATA)
        {
            long functionReturnValue = 0;

            long i = 0;
            long J = 0;
            byte[] v = null;
            v = strToToHexByte(DATA);

            //1.预置1个16位的寄存器为十六进制FFFF（即全为1）：称此寄存器为CRC寄存器；
            long CRC = 0;
            CRC = 0xffffL;
            for (i = 0; i <= (v).Length - 1; i++)
            {
                //2.把第一个8位二进制数据（既通讯信息帧的第一个字节）与16位的CRC寄存器的低8位相异或，把结果放于CRC寄存器；
                CRC = (CRC / 256) * 256L + (CRC % 256L) ^ v[i];
                for (J = 0; J <= 7; J++)
                {
                    //3.把CRC寄存器的内容右移一位（朝低位）用0填补最高位，并检查最低位；
                    //4.如果最低位为0：重复第3步（再次右移一位）；
                    // 如果最低位为1：CRC寄存器与多项式A001（1010 0000 0000 0001）进行异或；
                    //5.重复步骤3和4，直到右移8次，这样整个8位数据全部进行了处理；
                    long d0 = 0;
                    d0 = CRC & 1L;
                    CRC = CRC / 2;
                    if (d0 == 1)
                        CRC = CRC ^ 0xa001L;

                }

                //6.重复步骤2到步骤5，进行通讯信息帧下一字节的处理；
            }

            //7.最后得到的CRC寄存器内容即为：CRC码。
            CRC = CRC % 65536;
            functionReturnValue = CRC;

            return functionReturnValue;
        }
        #endregion

        public string ChangeOrder(string head, string businessid, string lableaddress, string waresum, string ls_string)
        {
            string ls_order = "";
            ls_order = head + businessid + lableaddress + waresum + ls_string;
            //CRC计算开始
            long lon = GetModBusCRC(ls_order);
            long h1, l0;
            h1 = lon % 256;
            l0 = lon / 256;

            string s = "";
            if (Convert.ToString(h1, 16).Length < 2)
            {
                s = "0" + Convert.ToString(h1, 16);
            }
            else
            {
                s = Convert.ToString(h1, 16);
            }

            if (Convert.ToString(l0, 16).Length < 2)
            {
                s = s + "0" + Convert.ToString(l0, 16);
            }
            else
            {
                s = s + Convert.ToString(l0, 16);
            }
            //CRC结束
            ls_order += s.ToUpper();
            ls_order = ChangeCrc(ls_order);
            return ls_order;
        }

        public string ChangeCrc(string ls_order)
        {
            string ls_neworder = "";
            for (int i = 0; i < ls_order.Length / 2; i++)
            {
                if (ls_order.Substring(i * 2, 2) == "7D")
                {
                    ls_neworder += "7D5D";
                }
                else if (ls_order.Substring(i * 2, 2) == "7E")
                {
                    ls_neworder += "7D5E";
                }
                else
                {
                    ls_neworder += ls_order.Substring(i * 2, 2);
                }
            }
            return ls_neworder;
        }

        public string ChangeCrc2(string ls_order)
        {
            string ls_neworder = "";
            //if (ls_order.Length <= 6)
            //{
            //    ls_neworder = ls_order;
            //    return ls_neworder;
            //}
            ////ls_order = ls_order.Substring(4, ls_order.Length - 6);
            ls_order = ls_order.Replace("7D", "7D5D");
            ls_order = ls_order.Replace("7E", "7D5E");
            ls_neworder = ls_order;
            return ls_neworder;
        }

        public string SendFile(string message)
        {
            TcpClient client = new TcpClient(address, host);
            NetworkStream stream = client.GetStream();
            try
            {
                //1.发送数据   
                //byte[] messages = strToHexByte(message);
                byte[] messages = Encoding.ASCII.GetBytes(message);//ASC发送
                stream.WriteTimeout = timers;//发送时间
                stream.Write(messages, 0, messages.Length);
                //2.接收状态,长度<1024字节
                byte[] bytes = new Byte[1024];
                string data = string.Empty;
                stream.ReadTimeout = timers;//接收返回信息
                int length = stream.Read(bytes, 0, bytes.Length);
                if (length > 0)
                {
                    data = ToHexString(bytes);//十六进制接收
                    if (data.Substring(0, 8) == "53535353" || data.Substring(0, 8) == "35353535")//字符串接收
                    {
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, length);
                        data = data.Substring(8, data.Length - 8);
                        return data;
                    }
                }
                //3.关闭对象
                return data;
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }

        public string bYtesToString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString());
                }
                hexString = strB.ToString();
            }
            return hexString;
        }

        public string TransAction(string str)
        {
            string ls_msg;

            try
            {
                ls_msg = str;
                return SendFile(ls_msg);//发送文件
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            finally
            {
                System.GC.Collect();
            }
        }

        #region 公用函数
        public void WriteLog(string ls_operuser, DateTime ls_opertime, string ls_text, string ls_text2, string ls_text3, string ls_text4, string ls_readtext, string ls_sendtext, string funcid, string ls_status)
        {
            string ls_sql;
            Open();
            BeginTrans();
            if (ls_text == String.Empty || ls_text == null)
            {
                ls_text = " ";
            }
            if (ls_text2 == String.Empty || ls_text2 == null)
            {
                ls_text2 = " ";
            }
            if (ls_text3 == String.Empty || ls_text3 == null)
            {
                ls_text3 = " ";
            }
            if (ls_text4 == String.Empty || ls_text4 == null)
            {
                ls_text4 = " ";
            }
            if (ls_readtext == String.Empty || ls_readtext == null)
            {
                ls_readtext = " ";
            }
            if (ls_sendtext == String.Empty || ls_sendtext == null)
            {
                ls_sendtext = " ";
            }
            ls_text = ls_text.Replace("'", "''");
            ls_text2 = ls_text2.Replace("'", "''");
            ls_text3 = ls_text3.Replace("'", "''");
            ls_text4 = ls_text4.Replace("'", "''");
            ls_readtext = ls_readtext.Replace("'", "''");
            ls_sendtext = ls_sendtext.Replace("'", "''");


            ls_sql = @"insert into Tb_Wms_webserviceInterfaceLog (LOGGUID,
                                           LOGID,
                                           LOGTEXT,
                                           LOGTEXTD,
                                           LOGTEXT2,
                                           LOGTEXT3,
                                           OPERDATE,
                                           OPERUSER,
                                           REMARK,
                                           SENDTO,
                                           READTO,
                                            FuncID,
                                            status)
                                           values
                                           (
                                           createguid(),
                                           (select nvl(max(logid),0) + 1 from Tb_Wms_webserviceInterfaceLog),
                                           :exper,               
                                           :exper3,                
                                           :exper2,              
                                           :exper4,              
                                           sysdate,               
                                           '" + ls_operuser + @"',               
                                           null,               
                                           '" + ls_sendtext + @"',               
                                           '" + ls_readtext + @"',              
                                           '" + funcid + @"',
                                           '" + ls_status + @"'                                           
                                           )";
            //ls_returnmsg = SqlDataTable(ls_sql);
            cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(ls_sql, cn);
            OracleParameter op = new OracleParameter("exper", OracleDbType.Clob);
            op.Value = ls_text;
            OracleParameter op3 = new OracleParameter("exper3", OracleDbType.Clob);
            op3.Value = ls_text3;
            OracleParameter op2 = new OracleParameter("exper2", OracleDbType.Clob);
            op2.Value = ls_text2;
            OracleParameter op4 = new OracleParameter("exper4", OracleDbType.Clob);
            op4.Value = ls_text4;
            cmd.Parameters.Add(op);
            cmd.Parameters.Add(op3);
            cmd.Parameters.Add(op2);
            cmd.Parameters.Add(op4);
            cmd.ExecuteNonQuery();
            CommitTrans();
            Close();
        }

        public string SqlDataTableCommit(string strSql)
        {
            string ls_return = "";
            Oracle.ManagedDataAccess.Client.OracleTransaction ston = null;
            Oracle.ManagedDataAccess.Client.OracleCommand cmdd = null;
            try
            {
                Open();
                BeginTrans();
                if (!inTransaction)
                {
                    ston = cn.BeginTransaction();
                    inTransaction = true;
                }
                cmdd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                cmdd.Connection = this.cn;

                if (inTransaction)
                {
                    cmdd.Transaction = trans;
                }
                else
                {
                    cmdd.Transaction = ston;
                }

                cmdd.CommandText = strSql;
                cmdd.ExecuteNonQuery();
                cmdd.Transaction.Commit();

            }
            catch (Exception ex)
            {
                ls_return = ex.Message.ToString();
                if (!inTransaction && cn.State.ToString().ToUpper() == "OPEN")
                {
                    ston.Rollback();
                }

                cmdd = null;
                return (ls_return);
            }
            finally
            {
                cmdd = null;
            }
            return ("TRUE");
        }

        public string Doprocedure(string proname, string[] inparam, string[] inparamvalue, string[] inparamtype, string[] outparam, string[] outparamtype, out string ls_returnxml, int func, bool ib_commit)
        {
            cmd = cn.CreateCommand();
            OracleParameter param = null;
            ls_returnxml = "";
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            OracleDataAdapter da = null;
            ib_commit = false;
            string ls_cursorname = "";
            string ls_text = "";
            try
            {
                Open();
                BeginTrans();
                if (inTransaction)
                {
                    cmd.Transaction = trans;
                }
                cmd.CommandText = proname;
                cmd.CommandType = CommandType.StoredProcedure;
                for (int i = 0; i < inparam.Length; i++)
                {
                    if (inparamtype[i] == "int")
                    {
                        param = cmd.Parameters.Add(new OracleParameter(inparam[i], OracleDbType.Int32, 8));
                    }
                    else if (inparamtype[i] == "varchar")
                    {
                        param = cmd.Parameters.Add(new OracleParameter(inparam[i], OracleDbType.Varchar2, 400));
                    }
                    param.Direction = ParameterDirection.Input;
                    param.Value = inparamvalue[i];
                }

                for (int i = 0; i < outparam.Length; i++)
                {
                    if (outparamtype[i] == "int")
                    {
                        param = cmd.Parameters.Add(new OracleParameter(outparam[i], OracleDbType.Int32, 4));
                    }
                    else if (outparamtype[i] == "varchar")
                    {
                        param = cmd.Parameters.Add(new OracleParameter(outparam[i], OracleDbType.Varchar2, 400));
                    }
                    else if (outparamtype[i] == "cursor")
                    {
                        param = cmd.Parameters.Add(new OracleParameter(outparam[i], OracleDbType.RefCursor, 400));
                        ls_cursorname = outparam[i];
                    }
                    param.Direction = ParameterDirection.Output;
                    param.Value = ls_text.PadRight(400, ' ');
                }
                cmd.ExecuteNonQuery();
                returncode = Convert.ToInt32(cmd.Parameters["as_returncode"].Value.ToString());
                returnmsg = Convert.ToString(cmd.Parameters["as_returnmsg"].Value.ToString());
                if (returncode != 0)
                {
                    RollbackTrans();
                    return (returnmsg);
                }
                if (ls_cursorname != "")
                {
                    if (func == 1001 || func == 1002 || func == 1060 || func == 1061 || func == 1009 || func == 1010 || func == 1013 || func == 1014 || func == 1015 || func == 1019 || func == 1022 || func == 1023 || func == 1033 || func == 1024 || func == 1026 || func == 1027 || func == 1036 || func == 1037 || func == 1046 || func == 1048 || func == 1049 || func == 1050 || func == 1052 || func == 1053 || func == 1054 || func == 1056 || func == 1057 || func == 1101 || func == 1103 || func == 1188 || func == 1039 || func == 1040 || func == 1062 || func == 1063)
                    {
                        da = new OracleDataAdapter(cmd);
                        da.TableMappings.Add("Table", ls_cursorname);
                        da.Fill(ds);
                        dt = ds.Tables[0];
                        if (dt.Rows.Count == 0)
                        {
                            if (func == 1002)
                            {
                                return ("服务器找不到相关任务");
                            }
                            else
                            {
                                WriteXml(dt);
                                ls_returnxml = doc.InnerXml;
                            }
                        }
                        else
                        {
                            WriteXml(dt);
                            ls_returnxml = doc.InnerXml;
                        }
                    }
                }
                else
                {
                    if (func == 1003)
                    {
                        ls_returnxml += "<?xml version='1.0' encoding='gb2312'?>";
                        ls_returnxml += "<function>";
                        ls_returnxml += "<data rowcount='1' columns='3'>";
                        ls_returnxml += "<row rownum='0'>";
                        ls_returnxml += "<column colnum='0' colname='checkguid'>" + Convert.ToString(cmd.Parameters["as_checkguid"].Value.ToString()) + "</column>";
                        ls_returnxml += "<column colnum='1' colname='inguid'>" + Convert.ToString(cmd.Parameters["as_inguid"].Value.ToString()) + "</column>";
                        ls_returnxml += "<column colnum='2' colname='flag'>" + Convert.ToString(cmd.Parameters["as_flag"].Value.ToString()) + "</column>";
                        ls_returnxml += "</row>";
                        ls_returnxml += "</data>";
                        ls_returnxml += "</function>";
                    }
                    else
                    {
                        ls_returnxml = "";
                    }
                }
                if (ib_commit)
                {
                    CommitTrans();
                }
                return ("TRUE");
            }
            catch (Exception ex)
            {
                RollbackTrans();
                returnmsg = ex.Message.ToString();
                return (returnmsg);
            }
            finally
            {
                cmd = null;
                if (da != null)
                {
                    da.Dispose();
                }
                if (ds != null)
                {
                    ds.Dispose();
                }
                if (dt != null)
                {
                    dt.Dispose();
                }
            }
        }

        public DataSet GetDataSet(string QueryString)
        {
            DataSet ds = null;
            Oracle.ManagedDataAccess.Client.OracleCommand cmdcur = null;
            OracleDataAdapter ad = null;
            try
            {
                Open();
                BeginTrans();
                cmdcur = new Oracle.ManagedDataAccess.Client.OracleCommand();
                cmdcur.Connection = this.cn;
                if (inTransaction)
                    cmdcur.Transaction = trans;
                ds = new DataSet();
                ad = new OracleDataAdapter();
                cmdcur.CommandText = QueryString;
                ad.SelectCommand = cmdcur;
                ad.Fill(ds);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                cmdcur = null;
                ad = null;
            }
            return (ds);
        }

        public DataTable GetDataTable(string QueryString)
        {
            DataSet ds = GetDataSet(QueryString);
            if (ds != null)
            {
                if (ds.Tables.Count > 0)
                {
                    return (ds.Tables[0]);
                }
                else
                {
                    return (new DataTable());
                }
            }
            else
            {
                return (new DataTable());
            }
        }

        public string convertstring(string str/*, DataColumn dtcol*/)
        {
            str = "'" + str + "'";
            return str;
        }

        /*public string InsertAdapter(DataTable dt, string tablename, int row, string columnname, string guid)
        {
            string sql = "insert into " + tablename + "  (" + columnname + ",";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i < dt.Columns.Count - 1)
                    sql += dt.Columns[i].ColumnName + ",";
                else
                    sql += dt.Columns[i].ColumnName;
            }
            sql += " ) values ('" + guid + "',";

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i < dt.Columns.Count - 1)
                    sql += convertstring(dt.Rows[row][i].ToString().Trim()) + ",";
                else
                    sql += convertstring(dt.Rows[row][i].ToString().Trim());
            }
            sql += " )";

            return (sql);
        }*/

        public string InsertAdapter(DataTable dt, string tablename, int row)
        {
            DataTable dtcur = new DataTable();
            dtcur = GetDataTable("select * from " + tablename + " where 1 = 2");
            string sql = "insert into " + tablename + "  (";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i < dt.Columns.Count - 1)
                    sql += dt.Columns[i].ColumnName + ",";
                else
                    sql += dt.Columns[i].ColumnName;
            }
            sql += " ) values (";

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(string))
                {
                    if (i < dt.Columns.Count - 1)
                        sql += convertstring(dt.Rows[row][i].ToString().Trim()) + ",";
                    else
                        sql += convertstring(dt.Rows[row][i].ToString().Trim());
                }
                else if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(DateTime))
                {
                    if (i < dt.Columns.Count - 1)
                        if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                        {
                            sql += "null,";
                        }
                        else
                        {
                            sql += "to_date(" + convertstring(dt.Rows[row][i].ToString().Trim()) + ",'yyyymmddhh24miss')" + ",";
                        }
                    else
                        if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                    {
                        sql += "null";
                    }
                    else
                    {
                        sql += "to_date(" + convertstring(dt.Rows[row][i].ToString().Trim()) + ",'yyyymmddhh24miss')";
                    }
                }
                else
                {
                    if (i < dt.Columns.Count - 1)
                        if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                        {
                            sql += "null,";
                        }
                        else
                        {
                            sql += dt.Rows[row][i].ToString().Trim() + ",";
                        }
                    else
                        if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                    {
                        sql += "null";
                    }
                    else
                    {
                        sql += dt.Rows[row][i].ToString().Trim();
                    }
                }
            }
            sql += " )";

            return (sql);
        }

        public string DeleteAdapter(DataTable dt, string tablename, int row)
        {
            string sql = "delete " + tablename + " where ";
            sql += dt.Columns[0].ColumnName + " = ";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i < dt.Columns.Count - 1)
                    sql += convertstring(dt.Rows[row][i].ToString().Trim()) + ",";
                else
                    sql += convertstring(dt.Rows[row][i].ToString().Trim());
            }
            sql += " ";

            return (sql);
        }

        public string FindAdapter(DataTable dt, int row, string sql)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                sql += convertstring(dt.Rows[row][i].ToString().Trim()) + ",";
            }

            return (sql);
        }

        public string UpdateAdapter(DataTable dt, string tablename, int row)
        {
            DataTable dtcur = new DataTable();
            dtcur = GetDataTable("select * from " + tablename + " where 1 = 2");
            string sql = "update " + tablename + " set ";
            DataColumn[] dtcols = dt.PrimaryKey;
            if (dtcols.Length == 0)
            {
                return "该表没有主键,无法生成修改语句";
            }
            else
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    bool iskey = false;
                    for (int k = 0; k < dtcols.Length; k++)
                    {
                        if (dt.Columns[i].ColumnName.Trim() == dtcols[k].ColumnName.Trim())
                            iskey = true;
                    }

                    if (!iskey)
                    {
                        if (i < dt.Columns.Count - 1)
                        {
                            if (dt.Rows[row][i].ToString().Trim() != "noupdate")
                            {
                                if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(string))
                                {
                                    sql += dt.Columns[i].ColumnName + "=" + convertstring(dt.Rows[row][i].ToString().Trim()) + ", ";
                                }
                                else if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(DateTime))
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + "null,";
                                    }
                                    else
                                    {
                                        sql += dt.Columns[i].ColumnName + "= to_date(" + convertstring(dt.Rows[row][i].ToString().Trim()) + ",'yyyymmddhh24miss'),";
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + "null,";
                                    }
                                    else
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + dt.Rows[row][i].ToString().Trim() + ", ";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (dt.Rows[row][i].ToString().Trim() != "noupdate")
                            {
                                if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(string))
                                {
                                    sql += dt.Columns[i].ColumnName + "=" + convertstring(dt.Rows[row][i].ToString().Trim());
                                }
                                else if (dtcur.Columns[dt.Columns[i].ColumnName].DataType == typeof(DateTime))
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + "null";
                                    }
                                    else
                                    {
                                        sql += dt.Columns[i].ColumnName + "=to_date(" + convertstring(dt.Rows[row][i].ToString().Trim()) + ",'yyyymmddhh24miss')";
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[row][i].ToString().Trim()))
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + "null";
                                    }
                                    else
                                    {
                                        sql += dt.Columns[i].ColumnName + "=" + dt.Rows[row][i].ToString().Trim();
                                    }
                                }
                            }
                            else
                            {
                                sql = sql.Substring(0, sql.Trim().Length - 1);
                            }
                        }
                    }
                }

                sql += " where ";

                for (int j = 0; j < dtcols.Length; j++)
                {
                    if (j == 0)
                        sql += dtcols[j].ColumnName.Trim() + "=" + convertstring(dt.Rows[row][dtcols[j].ColumnName.Trim()].ToString().Trim()) + "  ";
                    else
                        sql += " and " + dtcols[j].ColumnName.Trim() + "=" + convertstring(dt.Rows[row][dtcols[j].ColumnName.Trim()].ToString().Trim()) + " ";
                }
            }

            return (sql);
        }

        /// <summary> 
        /// 中文转化为GUID（先插入基础字典后转化）
        /// </summary> 
        /// <param name="dt">需转化的数据</param>
        /// <param name="functionid">函数功能ID</param> 
        /// <param name="dbtype">数据操作类型</param> 
        /// <param name="dtnew">转化后的数据</param> 
        /// <returns>返回值</returns>
        public string data_base_do(DataTable dt, string functionid, string dbtype, out DataTable dtnew)
        {
            string ls_name;
            string ls_msg;
            string ls_productareaguid;
            string ls_unitguid;
            string ls_guid;
            long ll_maxproductareacode;
            long ll_maxunitcode;
            string ls_maxproductareacode;
            string ls_maxunitcode;
            DataTable dtcur = new DataTable();
            dtnew = dt;
            if (functionid == "2002")//机构商品目录
            {
                dtcur = GetDataTable("select max(productareacode) from tb_wms_productarea");
                ls_maxproductareacode = dtcur.Rows[0][0].ToString();
                if (string.IsNullOrEmpty(ls_maxproductareacode))
                {
                    ll_maxproductareacode = 0;
                }
                else
                {
                    ll_maxproductareacode = Convert.ToInt32(ls_maxproductareacode);
                }
                dtcur = GetDataTable("select max(unitcode) from Tb_Wms_WareUnit");
                ls_maxunitcode = dtcur.Rows[0][0].ToString();
                if (string.IsNullOrEmpty(ls_maxunitcode))
                {
                    ll_maxunitcode = 0;
                }
                else
                {
                    ll_maxunitcode = Convert.ToInt32(ls_maxunitcode);
                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (dt.Columns[j].ColumnName.ToLower() == "productareaguid")
                        {
                            ls_name = dt.Rows[i]["productareaguid"].ToString();
                            if (ls_name == string.Empty && ls_name.Trim() == "")
                            {
                                dtnew.Rows[i][j] = null;
                                continue;
                            }
                            dtcur = GetDataTable("select productareaguid from tb_wms_productarea where ProductAreaName = '" + ls_name + "'");
                            if (dtcur.Rows.Count == 0)
                            {
                                dtcur = GetDataTable("select createguid() from dual");
                                ls_productareaguid = dtcur.Rows[0][0].ToString();
                                ll_maxproductareacode++;
                                ls_maxproductareacode = ll_maxproductareacode.ToString().PadLeft(5, '0');
                                ls_msg = SqlDataTable("insert into tb_wms_productarea(productareaguid,ProductAreaCode,ProductAreaName) values('" + ls_productareaguid + "','" + ls_maxproductareacode + "','" + ls_name + "')");
                                if (ls_msg != "TRUE")
                                {
                                    return ls_msg;
                                }
                                ls_guid = ls_productareaguid;
                            }
                            else
                            {
                                ls_productareaguid = dtcur.Rows[0][0].ToString();
                                ls_guid = ls_productareaguid;
                            }
                            dtnew.Rows[i][j] = ls_guid;
                        }
                        else if (dt.Columns[j].ColumnName.ToLower() == "unitguid")
                        {
                            ls_name = dt.Rows[i]["unitguid"].ToString();
                            if (ls_name == string.Empty && ls_name.Trim() == "")
                            {
                                dtnew.Rows[i][j] = null;
                                continue;
                            }
                            dtcur = GetDataTable("select UnitGUID from Tb_Wms_WareUnit where UnitName = '" + ls_name + "'");
                            if (dtcur.Rows.Count == 0)
                            {
                                dtcur = GetDataTable("select createguid() from dual");
                                ls_unitguid = dtcur.Rows[0][0].ToString();
                                ll_maxunitcode++;
                                ls_maxunitcode = ll_maxunitcode.ToString().PadLeft(5, '0');
                                ls_msg = SqlDataTable("insert into Tb_Wms_WareUnit(UnitGUID,UnitCode ,UnitName) values('" + ls_unitguid + "','" + ls_maxunitcode + "','" + ls_name + "')");
                                if (ls_msg != "TRUE")
                                {
                                    return ls_msg;
                                }
                                ls_guid = ls_unitguid;
                            }
                            else
                            {
                                ls_unitguid = dtcur.Rows[0][0].ToString();
                                ls_guid = ls_unitguid;
                            }
                            dtnew.Rows[i][j] = ls_guid;
                        }
                    }
                }
            }
            else if (functionid == "2011")//销售出库订单明细
            {
                string ls_orgguid;
                string ls_wareguid;
                string ls_batchs;
                long l_singnum;
                long l_packnum;
                long l_realsingle;
                long l_realpack;
                long l_PackNumnew;
                long l_singnum2;
                long l_packnum2;
                long l_KeepsingleNum;
                long l_keeppacknum;
                long l_orderKeepsingleNum;
                long l_orderkeeppacknum;
                long l_realsinglesum;
                long l_data;
                long l_ceilnum;
                long l_setpack;
                long l_setsingle;
                dtcur = GetDataTable("select orgguid from Tb_Wms_SaleOutstoreOrder where OrderGUID = '" + dt.Rows[0]["OrderGUID"] + "'");
                ls_orgguid = dtcur.Rows[0][0].ToString();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ls_wareguid = dt.Rows[i]["wareguid"].ToString();
                    ls_batchs = dt.Rows[i]["batchs"].ToString();
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(WareStoreSingleNum,0)),0),COALESCE(sum(nvl(WareStorePackNum,0)),0) FROM Tb_Wms_OrgStore WHERE orgguid = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "' And BATCHS = '" + ls_batchs + "'");
                    l_singnum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_packnum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(KeepsingleNum,0)),0),COALESCE(sum(nvl(KeepNum,0)),0) FROM Tb_Wms_PickKeepStore WHERE OrgGUID = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "'And BATCHS = '" + ls_batchs + "'");
                    l_KeepsingleNum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_keeppacknum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(KeepsingleNum,0)),0),COALESCE(sum(nvl(KeepNum,0)),0) FROM Tb_Wms_orderKeepStore WHERE OrgGUID = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "' And BATCHS = '" + ls_batchs + "'");
                    l_orderKeepsingleNum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_orderkeeppacknum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    l_singnum2 = l_singnum - l_KeepsingleNum - l_orderKeepsingleNum;
                    l_packnum2 = l_packnum - l_keeppacknum - l_orderkeeppacknum;

                    l_realsingle = l_singnum2;
                    l_realpack = l_packnum2;
                    dtcur = GetDataTable("SELECT PackNum FROM tb_wms_wmsware Where WAREGUID = '" + ls_wareguid + "'");
                    l_PackNumnew = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_realsinglesum = l_realpack * l_PackNumnew + l_realsingle;
                    l_data = Convert.ToInt32(dt.Rows[i]["SingleNum"]);
                    if (l_data > l_realsinglesum)
                    {
                        return "商品【" + dt.Rows[i]["warename"].ToString() + "】库存不足";
                    }

                    if (l_packnum == 0)
                    {
                        dtnew.Rows[i]["singlenum"] = l_data;
                        dtnew.Rows[i]["packnum"] = 0;
                    }
                    else
                    {
                        l_ceilnum = Convert.ToInt32(l_data / l_packnum);
                        if (l_ceilnum > l_realpack)
                        {
                            l_setpack = l_realpack;
                            l_setsingle = l_data - l_setpack * l_packnum;
                        }
                        else
                        {
                            l_setpack = l_ceilnum;
                            l_setsingle = l_data - l_setpack * l_packnum;
                        }
                        dtnew.Rows[i]["singlenum"] = l_setsingle;
                        dtnew.Rows[i]["packnum"] = l_setpack;
                    }
                }
            }
            else if (functionid == "2013")//购进退货出库订单明细
            {
                string ls_orgguid;
                string ls_wareguid;
                string ls_batchs;
                long l_singnum;
                long l_packnum;
                long l_realsingle;
                long l_realpack;
                long l_PackNumnew;
                long l_singnum2;
                long l_packnum2;
                long l_KeepsingleNum;
                long l_keeppacknum;
                long l_orderKeepsingleNum;
                long l_orderkeeppacknum;
                long l_realsinglesum;
                long l_data;
                long l_ceilnum;
                long l_setpack;
                long l_setsingle;
                dtcur = GetDataTable("select orgguid from Tb_Wms_SaleOutstoreOrder where OrderGUID = '" + dt.Rows[0]["OrderGUID"] + "'");
                ls_orgguid = dtcur.Rows[0][0].ToString();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    ls_wareguid = dt.Rows[i]["wareguid"].ToString();
                    ls_batchs = dt.Rows[i]["batchs"].ToString();
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(WareStoreSingleNum,0)),0),COALESCE(sum(nvl(WareStorePackNum,0)),0) FROM Tb_Wms_OrgStore WHERE orgguid = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "' And BATCHS = '" + ls_batchs + "'");
                    l_singnum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_packnum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(KeepsingleNum,0)),0),COALESCE(sum(nvl(KeepNum,0)),0) FROM Tb_Wms_PickKeepStore WHERE OrgGUID = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "'And BATCHS = '" + ls_batchs + "'");
                    l_KeepsingleNum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_keeppacknum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    dtcur = GetDataTable("SELECT COALESCE(sum(nvl(KeepsingleNum,0)),0),COALESCE(sum(nvl(KeepNum,0)),0) FROM Tb_Wms_orderKeepStore WHERE OrgGUID = '" + ls_orgguid + "' AND wareguid = '" + ls_wareguid + "' And BATCHS = '" + ls_batchs + "'");
                    l_orderKeepsingleNum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_orderkeeppacknum = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][1]) : 0;
                    l_singnum2 = l_singnum - l_KeepsingleNum - l_orderKeepsingleNum;
                    l_packnum2 = l_packnum - l_keeppacknum - l_orderkeeppacknum;

                    l_realsingle = l_singnum2;
                    l_realpack = l_packnum2;
                    dtcur = GetDataTable("SELECT PackNum FROM tb_wms_wmsware Where WAREGUID = '" + ls_wareguid + "'");
                    l_PackNumnew = dtcur.Rows.Count > 0 ? Convert.ToInt32(dtcur.Rows[0][0]) : 0;
                    l_realsinglesum = l_realpack * l_PackNumnew + l_realsingle;
                    l_data = Convert.ToInt32(dt.Rows[i]["SingleNum"]);

                    if (l_packnum == 0)
                    {
                        dtnew.Rows[i]["singlenum"] = l_data;
                        dtnew.Rows[i]["packnum"] = 0;
                    }
                    else
                    {
                        l_ceilnum = Convert.ToInt32(l_data / l_packnum);
                        if (l_ceilnum > l_realpack)
                        {
                            l_setpack = l_realpack;
                            l_setsingle = l_data - l_setpack * l_packnum;
                        }
                        else
                        {
                            l_setpack = l_ceilnum;
                            l_setsingle = l_data - l_setpack * l_packnum;
                        }
                        dtnew.Rows[i]["singlenum"] = l_setsingle;
                        dtnew.Rows[i]["packnum"] = l_setpack;
                    }
                }
            }
            return "TRUE";
        }

        public string data_validation(DataTable dt, string functionid, string dbtype)
        {
            if (dt.Rows.Count == 0)
            {
                return "没有传入明细数据，请检查！";
            }
            if (functionid == "2002")//商品目录
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["warename"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品名称不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["bar"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品条码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["PackNum"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行件装数量不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["PackVolumn"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行件装体积不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["SingleVolumn"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单品体积不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["UnitGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单位不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["KindGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品类别不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["SaveKindGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行存储类别不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["ProductAreaGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行产地不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2006")//采购入库订单
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrgGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WmsCompanyGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行往来单位不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderType"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单类型不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderNo"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单编号不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2007")//采购入库订单明细
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["warebar"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品条码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品ID不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品名称不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2008")//销售退货入库订单
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrgGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WmsCompanyGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行往来单位不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderType"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单类型不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderNo"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单编号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["SaleOutstoreOrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行销售出库订单不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2009")//销售退货入库订单明细
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品ID不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品名称不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2010")//销售出库订单
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrgGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WmsCompanyGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行往来单位不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderType"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单类型不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单编号不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2011")//销售出库订单明细
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品ID不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品名称不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["Batchs"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行批号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["ExpDate"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行截至有效期不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2012")//购进退货出库订单
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrgGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WmsCompanyGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行往来单位不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单编号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行采购入库订单不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2013")//购进退货出库订单明细
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrderGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行订单号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品ID不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareNo"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["WareName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行商品名称不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["Batchs"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行批号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["ExpDate"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行截至有效期不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2014")//机构目录
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["OrgCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构编码不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["OrgName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行机构名称不能为空！";
                        }
                    }
                }
            }
            else if (functionid == "2015")//往来单位目录
            {
                if (dbtype == "1" || dbtype == "2")//新增、更新
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(dt.Rows[i]["NatureGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单位性质不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["KindGUID"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单位类别不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["CompanyCode"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单位编号不能为空！";
                        }
                        else if (string.IsNullOrEmpty(dt.Rows[i]["CompanyName"].ToString()))
                        {
                            return "第【" + (i + 1).ToString() + "】行单位名称不能为空！";
                        }
                    }
                }
            }
            return "TRUE";
        }

        public string InsertDataTable(DataTable dt, string TableName, string guidname)
        {
            string strSql = "";
            string ls_return = "";
            //string ls_guid = "";
            cmd = null;
            if (dt.Rows.Count > 0)
            {
                try
                {
                    Open();
                    BeginTrans();
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                    cmd.Connection = this.cn;
                    if (inTransaction)
                    {
                        cmd.Transaction = trans;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (guidname != "")//未传入GUID，自动生成
                        {
                            //ls_guid = getguid();
                            //if (ls_guid.Length != 38)
                            //{
                            //    ls_return = ls_guid;
                            //    RollbackTrans();
                            //    cmd = null;
                            //    return (ls_return);
                            //}
                            //strSql = InsertAdapter(dt, TableName, i, guidname, ls_guid);
                            ls_return = "未传入GUID";
                        }
                        else
                        {
                            strSql = InsertAdapter(dt, TableName, i);//传入了GUID
                        }
                        cmd.CommandText = strSql;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    RollbackTrans();
                    cmd = null;
                    return (ls_return);
                }
                finally
                {
                    cmd = null;
                }
            }
            ls_return = "TRUE";
            return (ls_return);
        }

        public string InsertOrUpdateDataTable(DataTable dt, string TableName, string guidname)
        {
            string strSql = "";
            string ls_return = "";
            string ls_guidvalue = "";
            DataTable dtcur = new DataTable();
            cmd = null;
            if (dt.Rows.Count > 0)
            {
                try
                {
                    Open();
                    BeginTrans();
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                    cmd.Connection = this.cn;
                    if (inTransaction)
                    {
                        cmd.Transaction = trans;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        ls_guidvalue = dt.Rows[i][guidname].ToString();
                        dtcur = GetDataTable("select " + guidname + " from " + TableName + " where " + guidname + " = '" + ls_guidvalue + "'");
                        if (dtcur.Rows.Count == 0)
                        {
                            strSql = InsertAdapter(dt, TableName, i);
                            cmd.CommandText = strSql;
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            strSql = UpdateAdapter(dt, TableName, i);
                            cmd.CommandText = strSql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    RollbackTrans();
                    cmd = null;
                    return (ls_return);
                }
                finally
                {
                    cmd = null;
                }
            }
            ls_return = "TRUE";
            return (ls_return);
        }

        public string SqlDataTable(DataTable dt)
        {
            string strSql = "";
            string ls_return = "";
            cmd = null;
            if (dt.Rows.Count > 0)
            {
                try
                {
                    Open();
                    BeginTrans();
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                    cmd.Connection = this.cn;
                    if (inTransaction)
                    {
                        cmd.Transaction = trans;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        strSql = dt.Rows[i][0].ToString();
                        cmd.CommandText = "begin\r\n" + strSql + "\r\nend;";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    RollbackTrans();
                    cmd = null;
                    return (ls_return);
                }
                finally
                {
                    cmd = null;
                }
            }
            ls_return = "TRUE";
            return (ls_return);
        }

        public string SqlDataTable(string strSql)
        {
            string ls_return = "";
            cmd = null;
            try
            {
                Open();
                BeginTrans();
                cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                cmd.Connection = this.cn;
                if (inTransaction)
                {
                    cmd.Transaction = trans;
                }
                cmd.CommandText = strSql;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ls_return = ex.Message.ToString();
                RollbackTrans();
                cmd = null;
                return (ls_return);
            }
            finally
            {
                cmd = null;
            }
            return ("TRUE");
        }

        public string DeleteDataTable(DataTable dt, string TableName)
        {
            string strSql = "";
            string ls_return = "";
            cmd = null;
            if (dt.Rows.Count > 0)
            {
                try
                {
                    Open();
                    BeginTrans();
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                    cmd.Connection = this.cn;
                    if (inTransaction)
                    {
                        cmd.Transaction = trans;
                    }


                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        strSql = DeleteAdapter(dt, TableName, i);
                        cmd.CommandText = strSql;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    RollbackTrans();
                    cmd = null;
                    return (ls_return);
                }
                finally
                {
                    cmd = null;
                }
            }
            return (ls_return);
        }

        public string FindDataTable(DataTable dt, string functionid, string dbtype, out string ls_xml)
        {
            string strSql = "";
            string sql = "";
            string ls_return = "";
            DataTable ddt = null;
            ls_xml = "";
            if (dt.Rows.Count > 0)
            {
                try
                {
                    switch (functionid)
                    {
                        case "2005":
                            sql = "select wareguid,warecode,warename ";
                            sql += " from tb_wms_wmsware where wareguid in( ";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                strSql = FindAdapter(dt, i, strSql);
                            }
                            break;
                        case "2017":
                            if (dbtype == "1")
                            {
                                sql = "select orderguid,CheckNo,CheckName,WareSum,NoWareSum,QualSingleSum,NoqualSingleSum,QualPackSum,NoqualPackSum,OperUser,OperDate,Remark ";
                                sql += " from TB_WMS_BUGINSTORECHECK where ORDERGUID in( ";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            else if (dbtype == "2")
                            {
                                sql = "select orderguid,CheckNo,CheckName,WareSum,NoWareSum,QualSingleSum,NoqualSingleSum,QualPackSum,NoqualPackSum,OperUser,OperDate,Remark ";
                                sql += " from Tb_Wms_SaleReturnInstoreCheck where ORDERGUID in( ";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            break;
                        case "2018":
                            if (dbtype == "1")
                            {
                                sql = "select (select orderguid from TB_WMS_BUGINSTORECHECK where checkguid = t.checkguid) as orderguid,CheckGUID,WareGUID,WareName,Units,FactArea,Specs,Batch,ExpDate,ProductDate,QualSingleNum,QualPackNum,NoqualSingleNum,NoqualPackNum,Remark ";
                                sql += " from TB_WMS_BUGINSTORECHECKdetail t where CheckGUID in(select checkguid from TB_WMS_BUGINSTORECHECK where orderguid in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            else if (dbtype == "2")
                            {
                                sql = "select (select orderguid from Tb_Wms_SaleReturnInstoreCheck where checkguid = t.checkguid) as orderguid,CheckGUID,WareGUID,WareName,Units,FactArea,Specs,Batch,ExpDate,ProductDate,QualSingleNum,QualPackNum,NoqualSingleNum,NoqualPackNum,Remark ";
                                sql += " from Tb_Wms_SaleReturnInstoreCheckd t where CheckGUID in(select checkguid from Tb_Wms_SaleReturnInstoreCheck where orderguid in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            break;
                        case "2019":
                            if (dbtype == "1")
                            {
                                sql = "select INVENTCONFIRMGUID,ORGGUID,STOREAREAGUID,FROMTYPE,OPERUSER,OPERDATE,CHECKUSER,CHECKDATA,REMARK ";
                                sql += " from TB_WMS_INVENTADD where InventConfirmGUID in( select InventConfirmGUID from Tb_Wms_InventConfirm where InventGUID in( select InventGUID from Tb_Wms_Invent where StartInventGUID in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            else if (dbtype == "2")
                            {
                                sql = "select INVENTCONFIRMGUID,ORGGUID,STOREAREAGUID,FROMTYPE,OPERUSER,OPERDATE,CHECKUSER,CHECKDATA,REMARK ";
                                sql += " from TB_WMS_INVENTdel where InventConfirmGUID in( select InventConfirmGUID from Tb_Wms_InventConfirm where InventGUID in( select InventGUID from Tb_Wms_Invent where StartInventGUID in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            break;
                        case "2020":
                            if (dbtype == "1")
                            {
                                sql = "select SheLveGUID,LocationGUID,WareGUID,batchs,InventaddPackNum,InventaddSingleNum,Remark ";
                                sql += " from Tb_Wms_InventAddDetail t where InventAddGUID in(select InventAddGUID from TB_WMS_INVENTADD where InventConfirmGUID in( select InventConfirmGUID from Tb_Wms_InventConfirm where InventGUID in( select InventGUID from Tb_Wms_Invent where StartInventGUID in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            else if (dbtype == "2")
                            {
                                sql = "select SheLveGUID,LocationGUID,WareGUID,batchs,InventdelPackNum,InventdelSingleNum,Remark ";
                                sql += " from Tb_Wms_InventdelDetail t where InventdelGUID in(select InventdelGUID from TB_WMS_INVENTdel where InventConfirmGUID in( select InventConfirmGUID from Tb_Wms_InventConfirm where InventGUID in( select InventGUID from Tb_Wms_Invent where StartInventGUID in(";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strSql = FindAdapter(dt, i, strSql);
                                }
                            }
                            break;
                    }
                    strSql = strSql.Substring(0, strSql.Length - 1) + " ";
                    switch (functionid)
                    {
                        case "2018":
                            sql += strSql + " )) and batch is not null";
                            break;
                        case "2019":
                            sql += strSql + " ))) ";
                            break;
                        case "2020":
                            sql += strSql + " )))) ";
                            break;
                        default:
                            sql += strSql + " ) ";
                            break;
                    }
                    ddt = GetDataTable(sql);
                    WriteXml(ddt);
                    ls_xml = doc.InnerXml;
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    return (ls_return);
                }
                finally
                {

                }
            }
            return ("TRUE");
        }

        public string FindDataTable(DataTable dt, out string ls_xml)
        {
            string strSql = "";
            string ls_return = "";
            ls_xml = "";
            DataTable ddt = null;
            try
            {
                strSql = dt.Rows[0][0].ToString();
                ddt = GetDataTable(strSql);
                WriteXml(ddt);
                ls_xml = doc.InnerXml;
            }
            catch (Exception ex)
            {
                ls_return = ex.Message.ToString();
                return (ls_return);
            }
            ls_return = "TRUE";
            return (ls_return);
        }

        public string UpdateDataTable(DataTable dt, string TableName)
        {
            string strSql = "";
            string ls_return = "";
            cmd = null;
            if (dt.Rows.Count > 0)
            {
                try
                {
                    Open();
                    BeginTrans();
                    cmd = new Oracle.ManagedDataAccess.Client.OracleCommand();
                    cmd.Connection = this.cn;
                    if (inTransaction)
                    {
                        cmd.Transaction = trans;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        strSql = UpdateAdapter(dt, TableName, i);
                        cmd.CommandText = strSql;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    ls_return = ex.Message.ToString();
                    RollbackTrans();
                    cmd = null;
                    return (ls_return);
                }
                finally
                {
                    cmd = null;
                }
            }
            ls_return = "TRUE";
            return (ls_return);
        }

        public void Open()
        {
            if (cn.State.ToString().ToUpper() != "OPEN")
                this.cn.Open();
        }

        public void Close()
        {
            if (cn.State.ToString().ToUpper() == "OPEN")
            {
                this.cn.Close();
            }
        }

        public void DisPose()
        {
            if (cn.State.ToString().ToUpper() == "OPEN")
            {
                this.cn.Close();
            }

            this.cn.Dispose();
            this.cn = null;
        }

        protected void BeginTrans()
        {
            if (trans == null)
            {
                trans = null;
                trans = cn.BeginTransaction();
                inTransaction = true;
            }
        }

        protected void CommitTrans()
        {
            if (trans != null)
            {
                try
                {
                    trans.Commit();
                    inTransaction = false;
                    Close();
                }
                catch { }
            }
            else
            {
                if (cn != null)
                {
                    Close();
                }
            }
        }

        protected void RollbackTrans()
        {
            if (trans != null)
            {
                try
                {
                    trans.Rollback();
                    inTransaction = false;
                    Close();
                }
                catch { }
            }
            else
            {
                if (cn != null)
                {
                    Close();
                }
            }
        }
        #endregion

        #region 解析通讯密匙
        /*
		 * / <summary>
		 * / MD5 32位加密
		 * / </summary>
		 * / <param name="str"></param>
		 * / <returns></returns>
		 */
        public string UserMd5(string str)
        {
            string cl = str;
            string pwd = "";
            MD5 md5 = MD5.Create(); /* 实例化一个md5对像 */
            /* 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　 */
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            /* 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得 */
            for (int i = 0; i < s.Length; i++)
            {
                /* 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 */
                pwd = pwd + s[i].ToString("X2");
            }
            return (pwd.ToUpper());
        }


        /*
         * / <summary>
         * / 获取消息序列号
         * / </summary>
         * / <returns>消息序列号</returns>
         */
        public string GetSequenceId()
        {
            return (DateTime.Now.ToString("YYYYMMDDHHmmssSSS"));
        }


        #region 字符串转Base64
        /*
		 * / <summary>
		 * / 字符串转Base64
		 * / </summary>
		 * / <param name="str">字符串</param>
		 * / <returns>Base64字符串</returns>
		 */
        public string String2Base64(string str)
        {
            byte[] byteBody = Encoding.UTF8.GetBytes(str);
            return (Convert.ToBase64String(byteBody));
        }


        #endregion

        /*
		 * / <summary>
		 * / Base64转字符串
		 * / </summary>
		 * / <param name="str">Base64字符串</param>
		 * / <returns>字符串</returns>
		 */
        public string Base642String(string str)
        {
            byte[] byteBody = Convert.FromBase64String(str);
            return (Encoding.UTF8.GetString(byteBody, 0, byteBody.Length));
        }


        #endregion

        #region readxml
        /* 构造函数，将内存流的数据存为xmlDocument */
        public void ReadXml(MemoryStream ms)
        {
            memory = ms;
            memory.Seek(0, SeekOrigin.Begin);
            xmlDoc.Load(memory);
        }


        /* 构造函数，将内存流的数据存为xmlDocument */
        public void ReadXml(string ls_xml)
        {
            //ls_xml = ls_xml.Replace("<![CDATA[", "").Replace("]]>", "");
            try
            {
                xmlDoc.LoadXml(ls_xml);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            finally
            {

            }
        }


        #region 解析编码
        public string function_id()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "functionid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }

            return (Getinfo);
        }

        public string org_no()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "orgcode")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_returncode()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "returncode")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_returnmsg()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "returnmsg")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_no()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "operusername")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_orderguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "orderguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_bartype()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "bartype")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_picktype()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "picktype")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_wareguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "wareguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_warepackbar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "warepackbar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_shelveguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "shelveguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_flag()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "flag")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_groundguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "groundguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_guid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "guid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_locationguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "locationguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_pickguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "pickguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_locationbar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "locationbar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_trayguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "trayguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_codebar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "codebar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_codetype()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "codetype")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_status()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "status")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_batchs()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "batchs")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_num()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "num")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_Transportguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "transportguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_boxguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "boxguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_checkguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "checkguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_boxbar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "boxbar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_bar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "bar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_type()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "type")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_usercardbar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "usercardbar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_inventguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "inventguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_confirmguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "confirmguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_order()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "order")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_traybar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "traybar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_warebar()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "warebar")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_inguid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "inguid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_interface()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "interface")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_model()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "model")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_labeladdress()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "labeladdress")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_businessid()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "businessid")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_time()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "opertime")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string oper_pass()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "operuserpass")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string user_no()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "interfaceusername")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string user_pass()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "interfaceuserpass")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string wms_no()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "wmscode")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        public string wms_dbtype()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "dbtype")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }
        #endregion

        /* 解析返回信息 */
        public void GetOutCode(ref string out_code, ref string outtext)
        {
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "returncode")
                {
                    out_code = xmlelem.InnerText;
                }
                else if (xmlelem.Name == "returnmsg")
                {
                    outtext = xmlelem.InnerText;
                }
            }
        }

        /* 解析单个SQL及参数 */
        public string GetSql()
        {
            string Getinfo = "";
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "SQL")
                {
                    Getinfo = xmlelem.InnerText;
                }
            }
            return (Getinfo);
        }

        /* 解析多个SQL及参数 */
        public ArrayList GetAllSQL()
        {
            int rowcount = 0;
            int rownum = 0;
            int i = 0;
            string SQLvalue = "";
            ArrayList arraySQL = new ArrayList();

            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "SQL")
                {
                    rowcount = int.Parse(xmlelem.GetAttribute("rowcount").ToString());
                    i = 0;
                    XmlNodeList SQLlist = (XmlNodeList)xmlnode.ChildNodes;
                    foreach (XmlNode rownode in SQLlist)
                    {
                        XmlElement xmlrow = (XmlElement)rownode;
                        if (xmlrow.Name == "row")
                        {
                            i++;
                            rownum = int.Parse(xmlrow.GetAttribute("rownum").ToString());
                            if (i == rownum)
                            {
                                SQLvalue = xmlrow.InnerText;
                                arraySQL.Add(SQLvalue);
                            }
                            else
                            {
                                throw new Exception("在第" + rownum.ToString() + "行出现错误！");
                            }
                        }
                    }

                    if (rowcount != arraySQL.Count)
                    {
                        throw new Exception("在读取数据时出现错误，行数与实际不符！");
                    }
                }
            }
            return (arraySQL);
        }

        public DataTable GetAllColumnsData(DataTable resourceDt)
        {
            if (resourceDt != null)
            {
                int rowcount = 0;
                int columns = 0;
                int rownum = 0;
                int colnum = 0;
                int i = 0;
                string columnvalue = "";
                int dtrownum = 0;
                DataTable dt = resourceDt;

                XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

                foreach (XmlNode xmlnode in nodeList)
                {
                    XmlElement xmlelem = (XmlElement)xmlnode;
                    if (xmlelem.Name == "data")
                    {
                        rowcount = int.Parse(xmlelem.GetAttribute("rowcount").ToString());
                        columns = int.Parse(xmlelem.GetAttribute("columns").ToString());

                        XmlNodeList datalist = (XmlNodeList)xmlnode.ChildNodes;

                        foreach (XmlNode rownode in datalist)
                        {
                            XmlElement xmlrow = (XmlElement)rownode;
                            if (xmlrow.Name == "row")
                            {
                                rownum = int.Parse(xmlrow.GetAttribute("rownum").ToString());

                                if (dtrownum == rownum)
                                {
                                    DataRow dtrow = dt.NewRow(); ;

                                    i = 0;
                                    XmlNodeList rowlist = (XmlNodeList)rownode.ChildNodes;
                                    foreach (XmlNode colnode in rowlist)
                                    {
                                        XmlElement xmlcolumn = (XmlElement)colnode;
                                        if (xmlcolumn.Name == "column")
                                        {
                                            colnum = int.Parse(xmlcolumn.GetAttribute("colnum").ToString());

                                            if (i == colnum)
                                            {
                                                columnvalue = xmlcolumn.InnerText;
                                                dtrow[colnum] = columnvalue.Replace("@@@@", " ");
                                                i++;
                                            }
                                            else
                                            {
                                                throw new Exception("在第" + rownum.ToString() + "行第" + i.ToString() + "列出现错误！");
                                            }
                                        }
                                    }
                                    dt.Rows.Add(dtrow);
                                    dtrownum++;
                                }
                                else
                                {
                                    throw new Exception("在第" + rownum.ToString() + "行出现错误！");
                                }
                            }
                        }
                    }
                }
                return (dt);
            }
            else
            {
                /* return GetAllColumnsData(); */
                return (null);
            }
        }

        public DataTable GetAllColumnsData()
        {
            int rowcount = 0;
            int columns = 0;
            int rownum = 0;
            int colnum = 0;
            int i = 0;
            string columnvalue = "";
            string colname = "";
            int dtrownum = 0;
            DataTable dt = new DataTable();

            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "data")
                {
                    rowcount = int.Parse(xmlelem.GetAttribute("rowcount").ToString());
                    columns = int.Parse(xmlelem.GetAttribute("columns").ToString());

                    for (i = 1; i <= columns; i++)
                    {
                        dt.Columns.Add("column" + i.ToString(), typeof(string));
                    }


                    XmlNodeList datalist = (XmlNodeList)xmlnode.ChildNodes;

                    foreach (XmlNode rownode in datalist)
                    {
                        XmlElement xmlrow = (XmlElement)rownode;
                        if (xmlrow.Name == "row")
                        {
                            rownum = int.Parse(xmlrow.GetAttribute("rownum").ToString());

                            if (dtrownum == rownum)
                            {
                                DataRow dtrow = dt.NewRow(); ;

                                i = 0;
                                XmlNodeList rowlist = (XmlNodeList)rownode.ChildNodes;
                                foreach (XmlNode colnode in rowlist)
                                {
                                    XmlElement xmlcolumn = (XmlElement)colnode;
                                    if (xmlcolumn.Name == "column")
                                    {
                                        colnum = int.Parse(xmlcolumn.GetAttribute("colnum").ToString());
                                        colname = xmlcolumn.GetAttribute("colname").ToString();
                                        if (i == colnum)
                                        {
                                            columnvalue = xmlcolumn.InnerText;
                                            dt.Columns[i].ColumnName = colname;
                                            dtrow[colnum] = columnvalue.Replace("@@@@", " ");
                                            i++;
                                        }
                                        else
                                        {
                                            throw new Exception("在第" + rownum.ToString() + "行第" + i.ToString() + "列出现错误！");
                                        }
                                    }
                                }
                                dt.Rows.Add(dtrow);
                                dtrownum++;
                            }
                            else
                            {
                                throw new Exception("在第" + rownum.ToString() + "行出现错误！");
                            }
                        }
                    }
                }
            }
            return dt;
        }

        public DataTable GetAllColumnsData(string[] ls_columnname, int primarykey)
        {
            int rowcount = 0;
            int columns = 0;
            int rownum = 0;
            int colnum = 0;
            int i = 0;
            string columnvalue = "";
            int dtrownum = 0;
            DataTable dt = new DataTable();

            XmlNodeList nodeList = xmlDoc.SelectSingleNode("function").ChildNodes;

            foreach (XmlNode xmlnode in nodeList)
            {
                XmlElement xmlelem = (XmlElement)xmlnode;
                if (xmlelem.Name == "data")
                {
                    rowcount = int.Parse(xmlelem.GetAttribute("rowcount").ToString());
                    columns = int.Parse(xmlelem.GetAttribute("columns").ToString());

                    for (i = 0; i <= ls_columnname.Length - 1; i++)
                    {
                        /* dt.Columns.Add("column" + i.ToString(), typeof(string)); */
                        dt.Columns.Add(ls_columnname[i], typeof(string));
                    }

                    dt.PrimaryKey = new DataColumn[] { dt.Columns[ls_columnname[primarykey]] };
                    XmlNodeList datalist = (XmlNodeList)xmlnode.ChildNodes;

                    foreach (XmlNode rownode in datalist)
                    {
                        XmlElement xmlrow = (XmlElement)rownode;
                        if (xmlrow.Name == "row")
                        {
                            rownum = int.Parse(xmlrow.GetAttribute("rownum").ToString());

                            if (dtrownum == rownum)
                            {
                                DataRow dtrow = dt.NewRow(); ;

                                i = 0;
                                XmlNodeList rowlist = (XmlNodeList)rownode.ChildNodes;
                                foreach (XmlNode colnode in rowlist)
                                {
                                    XmlElement xmlcolumn = (XmlElement)colnode;
                                    if (xmlcolumn.Name == "column")
                                    {
                                        colnum = int.Parse(xmlcolumn.GetAttribute("colnum").ToString());

                                        if (i == colnum)
                                        {
                                            columnvalue = xmlcolumn.InnerText;
                                            dtrow[colnum] = columnvalue;
                                            i++;
                                        }
                                        else
                                        {
                                            throw new Exception("在第" + rownum.ToString() + "行第" + i.ToString() + "列出现错误！");
                                        }
                                    }
                                }
                                dt.Rows.Add(dtrow);
                                dtrownum++;
                            }
                            else
                            {
                                throw new Exception("在第" + rownum.ToString() + "行出现错误！");
                            }
                        }
                    }
                }
            }
            return (dt);
        }
        #endregion

        #region writexml
        #region  将XmlDocument转化为string


        /*
		 * / <summary>
		 * / 将XmlDocument转化为string
		 * / </summary>
		 * / <param name="xmlDoc"></param>
		 * / <returns></returns>
		 */
        public string ConvertXmlToString(XmlDocument xml_Doc)
        {
            MemoryStream stream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(stream, null);
            writer.Formatting = Formatting.Indented;
            xml_Doc.Save(writer);
            StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            stream.Position = 0;
            string xmlString = sr.ReadToEnd();
            sr.Close();
            stream.Close();
            return (xmlString);
        }


        #endregion

        #region 输入请求
        /* 单个SQL或参数 */
        public void WriteXml(string function_id, string center_no, string hospital_sysno,
                      string SQL)
        {
            doc = new XmlDocument();
            /* 添加功能 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement function_id_element = doc.CreateElement("function_id");
            function_id_element.InnerText = function_id;

            XmlElement center_no_element = doc.CreateElement("center_no");
            center_no_element.InnerText = center_no;

            XmlElement hospital_sysno_element = doc.CreateElement("hospital_sysno");
            hospital_sysno_element.InnerText = hospital_sysno;

            XmlElement SQL_element = doc.CreateElement("SQL");
            SQL_element.InnerText = SQL;

            doc.DocumentElement.AppendChild(function_id_element); /* 添加function_id元素到根节点 */
            doc.DocumentElement.AppendChild(center_no_element);
            doc.DocumentElement.AppendChild(hospital_sysno_element);
            doc.DocumentElement.AppendChild(SQL_element);

            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }


        /* 返回值 */
        public void WriteTestXml(string functionid, string returncode, string returnmsg, string orgname)
        {
            doc = new XmlDocument();
            /* 添加功能 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement functionid_element = doc.CreateElement("functionid");
            functionid_element.InnerText = functionid;

            XmlElement returncode_element = doc.CreateElement("returncode");
            returncode_element.InnerText = returncode;

            XmlElement returnmsg_element = doc.CreateElement("returnmsg");
            returnmsg_element.InnerText = returnmsg;

            XmlElement orgname_element = doc.CreateElement("orgname");
            orgname_element.InnerText = orgname;

            doc.DocumentElement.AppendChild(functionid_element); /* 添加functionid元素到根节点 */
            doc.DocumentElement.AppendChild(returncode_element);
            doc.DocumentElement.AppendChild(returnmsg_element);
            doc.DocumentElement.AppendChild(orgname_element);

            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }


        /* 返回值 */
        public void WriteTestXml(string functionid, string returncode, string returnmsg)
        {
            doc = new XmlDocument();
            /* 添加功能 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement functionid_element = doc.CreateElement("functionid");
            functionid_element.InnerText = functionid;

            XmlElement returncode_element = doc.CreateElement("returncode");
            returncode_element.InnerText = returncode;

            XmlElement returnmsg_element = doc.CreateElement("returnmsg");
            returnmsg_element.InnerText = returnmsg;

            doc.DocumentElement.AppendChild(functionid_element); /* 添加functionid元素到根节点 */
            doc.DocumentElement.AppendChild(returncode_element);
            doc.DocumentElement.AppendChild(returnmsg_element);

            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }


        /* 单个SQL+datatable */
        public void WriteXml(string function_id, string center_no, string hospital_sysno,
                      string SQL, DataTable dt)
        {
            doc = new XmlDocument();
            /* 添加功能 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement function_id_element = doc.CreateElement("function_id");
            function_id_element.InnerText = function_id;

            XmlElement center_no_element = doc.CreateElement("center_no");
            center_no_element.InnerText = center_no;

            XmlElement hospital_sysno_element = doc.CreateElement("hospital_sysno");
            hospital_sysno_element.InnerText = hospital_sysno;

            XmlElement SQL_element = doc.CreateElement("SQL");
            SQL_element.InnerText = SQL;

            doc.DocumentElement.AppendChild(function_id_element); /* 添加function_id元素到根节点 */
            doc.DocumentElement.AppendChild(center_no_element);
            doc.DocumentElement.AppendChild(hospital_sysno_element);
            doc.DocumentElement.AppendChild(SQL_element);

            XmlElement dataelement = doc.CreateElement("data");
            /* 添加总行数 */
            dataelement.SetAttribute("rowcount", dt.Rows.Count.ToString());
            /* 添加总列数 */
            dataelement.SetAttribute("columns", dt.Columns.Count.ToString());

            /* 循环添加行和列 */
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                /* 添加行 */
                XmlElement addrow = doc.CreateElement("row");
                addrow.SetAttribute("rownum", i.ToString());
                dataelement.AppendChild(addrow);
                /* 添加列 */
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    XmlElement addcolume = doc.CreateElement("column");
                    addcolume.SetAttribute("colnum", j.ToString());
                    addcolume.SetAttribute("colname", dt.Columns[j].ColumnName);
                    addcolume.InnerText = dt.Rows[i][j].ToString();
                    addrow.AppendChild(addcolume);
                }
            }

            /* 添加所有元素到根节点 */
            try
            {
                doc.DocumentElement.AppendChild(dataelement);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }

        public string WriteXml2(DataTable dt)
        {
            doc = new XmlDocument();

            XmlElement dataelement = doc.CreateElement("data");
            /* 添加总行数 */
            dataelement.SetAttribute("rowcount", dt.Rows.Count.ToString());
            /* 添加总列数 */
            dataelement.SetAttribute("columns", dt.Columns.Count.ToString());

            /* 循环添加行和列 */
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                /* 添加行 */
                XmlElement addrow = doc.CreateElement("row");
                addrow.SetAttribute("rownum", i.ToString());
                dataelement.AppendChild(addrow);
                /* 添加列 */
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    XmlElement addcolume = doc.CreateElement("column");
                    addcolume.SetAttribute("colnum", j.ToString());
                    addcolume.SetAttribute("colname", dt.Columns[j].ColumnName);
                    addcolume.InnerText = dt.Rows[i][j].ToString();
                    addrow.AppendChild(addcolume);
                }
            }

            return dataelement.InnerXml.ToString();
        }


        /* 多个字段或参数 */
        public void WriteXml(string function_id, string center_no, string hospital_sysno,
                      ArrayList arraySQL)
        {
            doc = new XmlDocument();
            /* 添加根元素 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement function_id_element = doc.CreateElement("function_id");
            function_id_element.InnerText = function_id;


            XmlElement center_no_element = doc.CreateElement("center_no");
            center_no_element.InnerText = center_no;

            XmlElement hospital_sysno_element = doc.CreateElement("hospital_sysno");
            hospital_sysno_element.InnerText = hospital_sysno;

            XmlElement SQL_element = doc.CreateElement("SQL");
            /* 添加总数 */
            SQL_element.SetAttribute("rowcount", arraySQL.Count.ToString());

            /* 循环添加行和列 */
            for (int i = 1; i <= arraySQL.Count; i++)
            {
                /* 添加行 */
                XmlElement addrow = doc.CreateElement("row");
                addrow.SetAttribute("rownum", i.ToString());
                addrow.InnerText = (string)arraySQL[i - 1];
                SQL_element.AppendChild(addrow);
            }

            doc.DocumentElement.AppendChild(function_id_element); /* 添加function_id元素到根节点 */
            doc.DocumentElement.AppendChild(center_no_element);
            doc.DocumentElement.AppendChild(hospital_sysno_element);
            doc.DocumentElement.AppendChild(SQL_element);

            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");


            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);

        }


        #endregion

        #region 输出结果
        public void WriteXml(DataTable dt)
        {
            doc = new XmlDocument();
            /* 添加根元素 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement dataelement = doc.CreateElement("data");
            /* 添加总行数 */
            dataelement.SetAttribute("rowcount", dt.Rows.Count.ToString());
            /* 添加总列数 */
            dataelement.SetAttribute("columns", dt.Columns.Count.ToString());

            /* 循环添加行和列 */
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                /* 添加行 */
                XmlElement addrow = doc.CreateElement("row");
                addrow.SetAttribute("rownum", i.ToString());
                dataelement.AppendChild(addrow);
                /* 添加列 */
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    XmlElement addcolume = doc.CreateElement("column");
                    addcolume.SetAttribute("colnum", j.ToString());
                    addcolume.SetAttribute("colname", dt.Columns[j].ColumnName);
                    addcolume.InnerText = dt.Rows[i][j].ToString();
                    addrow.AppendChild(addcolume);
                }
            }

            /* 添加所有元素到根节点 */
            try
            {
                /*
                 * doc.DocumentElement.AppendChild(idelement); / * 添加function_id元素到根节点 * /
                 * doc.DocumentElement.AppendChild(codeelement);
                 * doc.DocumentElement.AppendChild(outelement);
                 */
                doc.DocumentElement.AppendChild(dataelement);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }


        public void WriteXml(string fun_id, string out_code, string outtext)
        {
            doc = new XmlDocument();
            /* 添加根元素 */
            XmlElement rootNode = doc.CreateElement("function");
            doc.AppendChild(rootNode);

            XmlElement idelement = doc.CreateElement("functionid");
            idelement.InnerText = fun_id;

            /* 添加out_code元素 */
            XmlElement codeelement = doc.CreateElement("returncode");
            codeelement.InnerText = out_code;

            XmlElement outelement = doc.CreateElement("returnmsg");
            outelement.InnerText = outtext;

            doc.DocumentElement.AppendChild(idelement); /*添加functionid元素到根节点 */
            doc.DocumentElement.AppendChild(codeelement);
            doc.DocumentElement.AppendChild(outelement);


            XmlProcessingInstruction xmlPI = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='gb2312'");
            doc.InsertBefore(xmlPI, doc.ChildNodes[0]);
        }


        #endregion

        public string GetStreamString(MemoryStream ms)
        {
            byte[] cache = new BinaryReader(ms).ReadBytes(Convert.ToInt32(ms.Length));
            return (Convert.ToBase64String(cache, 0, cache.Length));
        }



        #endregion
        #endregion

        #region 主接口
        [WebMethod(Description = "连接测试")]
        public string TestConnect(string requestParmXML, string uploadDataXML, out string retDataXML)
        {
            string ls_xml;
            string ls_msg;
            string ls_returnmsg;
            retDataXML = "";
            ReadXml(requestParmXML);
            OrgCode = org_no();
            controlip = wms_dbtype();
            InterfaceUserID = user_no();
            InterfacePassWord = user_pass();
            OperUserID = oper_no();
            OperPassWord = oper_pass();
            Opertime = oper_time();
            Interface = oper_interface();
            FunctionId = function_id();
            if (Interface != "MIAERP")
            {
                if (String2Base64(UserMd5(FunctionId + Opertime)) != Interface)
                {
                    WriteXml("0001", "2000", "通讯密匙验证失败");
                    return (doc.InnerXml);
                }
            }
            if (getcnParms(OrgCode, out ls_msg))
            {
                ls_returnmsg = checkUserValid(FunctionId, OrgCode, InterfaceUserID, InterfacePassWord, OperUserID, OperPassWord, ls_msg, 1, 1, out ls_xml);
                if (ls_returnmsg == "TRUE")
                {
                    WriteXml("0001", "0000", "测试成功");
                }
                else
                {
                    WriteXml("0001", "2001", ls_returnmsg);
                }
            }
            else
            {
                WriteXml("0001", "9100", ls_msg);
            }
            return (doc.InnerXml);
        }

        [WebMethod(Description = "门店调用接口处理业务的方法")]
        public string ShopInterface(string requestParmXML, string uploadDataXML, out string retDataXML)
        {
            #region 变量
            //string retDataXML = "";
            string ls_msg = "";
            string ls_returnmsg = "";
            string ls_pickguid = "";
            string ls_naturecode = "";
            string ls_returndt = "";
            string[] ls_inparam = null;
            string[] ls_inparamvalue = null;
            string[] ls_inparamtype = null;
            string[] ls_outparam = null;
            string[] ls_outparamtype = null;
            string ls_xml = "";
            string ls_bar = "";
            string ls_type = "";
            string ls_boxbar = "";
            string ls_picktype = "";
            string ls_traybar = "";
            string ls_inguid = "";
            string ls_orderguid = "";
            string ls_checkguid = "";
            string ls_groundguid = "";
            string ls_confirmguid = "";
            string ls_guid = "";
            string ls_flag = "";
            long ll_cnt = 0;
            string ls_inventnum = "";
            string ls_locationguid = "";
            string ls_getxml = "";
            string ls_controlip = "";
            string ls_opermsg = "";
            string ls_returncode = "";
            string ls_wareguid = "";
            string ls_warepackbar = "";
            string ls_inventguid = "";
            string ls_trayguid = "";
            string ls_groundnum = "";
            string ls_picknum = "";
            string ls_bartype = "";
            string ls_orgguid = "";
            string ls_usercardbar = "";
            DataTable dt = new DataTable();
            DataTable ddt = null;
            retDataXML = "";
            try
            {
                ReadXml(requestParmXML);
                OrgCode = org_no();
                controlip = wms_dbtype();
                InterfaceUserID = user_no();
                InterfacePassWord = user_pass();
                OperUserID = oper_no();
                OperPassWord = oper_pass();
                Opertime = oper_time();
                Interface = oper_interface();
                FunctionId = function_id();
                #endregion

                #region 密匙验证
                if (Interface != "MIAWMS")
                {
                    if (String2Base64(UserMd5(FunctionId + Opertime)) != Interface)
                    {
                        WriteXml("0001", "2000", "通讯密匙验证失败");
                        return (doc.InnerXml);
                    }
                }
                #endregion

                #region 获取数据库连接
                if (!getcnParms(OrgCode, out ls_msg))
                {
                    WriteXml(FunctionId, "9100", ls_msg);
                    return (doc.InnerXml);
                }
                #endregion

                #region 工号牌登陆1040
                if (FunctionId == "1040")
                {
                    ReadXml(uploadDataXML);
                    OperUserID = oper_usercardbar();
                    ls_usercardbar = oper_usercardbar();
                }
                #endregion

                #region 验证身份
                if (FunctionId == "1001" || FunctionId == "1002" || FunctionId == "1040") /* 需要身份验证的业务 */
                {
                    ls_returnmsg = checkUserValid(FunctionId, OrgCode, InterfaceUserID, InterfacePassWord, OperUserID, OperPassWord, ls_msg, 1 /*业务系统传2;PDA传1*/, 1, out ls_xml);
                }
                else
                {
                    ls_returnmsg = checkUserValid(FunctionId, OrgCode, InterfaceUserID, InterfacePassWord, OperUserID, OperPassWord, ls_msg, 1 /*业务系统传2;PDA传1*/, 0, out ls_xml);
                }
                if (ls_returnmsg != "TRUE")
                {
                    WriteXml(FunctionId, "2001", ls_returnmsg);
                    return (doc.InnerXml);
                }
                #endregion

                switch (FunctionId)
                {
                    default:
                        break;
                }
                #region 返回值处理
                if (ls_returnmsg != "TRUE")
                {
                    RollbackTrans();
                    WriteXml(FunctionId, "1000", ls_returnmsg);
                    WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "PDA", null, FunctionId, "1000");
                }
                else
                {
                    CommitTrans();
                    if (ls_returndt != "")
                    {
                        WriteXml(FunctionId, "0000", "处理成功");
                        retDataXML = ls_returndt;
                    }
                    else if (ls_xml != "" && ls_xml != null)
                    {
                        WriteXml(FunctionId, "0000", "处理成功");
                        retDataXML = ls_xml;
                    }
                    else
                    {
                        WriteXml(FunctionId, "0000", "处理成功");
                    }
                    WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "PDA", null, FunctionId, "0000");
                }
                return (doc.InnerXml);
                #endregion
            }
            catch (Exception ex)
            {
                RollbackTrans();
                ls_returnmsg = ex.Message.ToString();
                WriteXml(FunctionId, "1000", ls_returnmsg);
                WriteLog(OperUserID, DateTime.Now, requestParmXML, ls_returnmsg, uploadDataXML, doc.InnerXml, "PDA", null, FunctionId, "1000");
                return (doc.InnerXml);
            }
        }

        [WebMethod(Description = "分公司调用接口处理业务的方法")]
        public string CompanyInterface(string requestParmXML, string uploadDataXML, out string retDataXML)
        {
            #region 变量
            string ls_msg = "";
            string ls_returnmsg = "";
            string ls_returndt = "";
            string[] ls_columnname = null;
            string ls_xml;
            string ls_dbtype;
            string ls_orderguid = "";
            string ls_orgguid = "";
            //string retDataXML = "";
            ReadXml(requestParmXML);
            OrgCode = org_no();
            ls_dbtype = wms_dbtype();
            InterfaceUserID = user_no();
            InterfacePassWord = user_pass();
            OperUserID = oper_no();
            OperPassWord = oper_pass();
            Opertime = oper_time();
            Interface = oper_interface();
            FunctionId = function_id();
            retDataXML = "";
            #endregion
            try
            {
                #region 初始化
                if (Interface != "MIAWMS")
                {
                    if (String2Base64(UserMd5(FunctionId + Opertime)) != Interface)
                    {
                        WriteXml("0001", "2000", "通讯密匙验证失败");
                        return (doc.InnerXml);
                    }
                }
                #endregion
                if (getcnParms(OrgCode, out ls_msg))
                {
                    ls_returnmsg = checkUserValid(FunctionId, OrgCode, InterfaceUserID, InterfacePassWord, OperUserID, OperPassWord, ls_msg, 2 /*业务系统传2;PDA传1*/, 0/*不需要用户身份验证*/, out ls_xml);
                    if (ls_returnmsg == "TRUE")
                    {
                        DataTable dt = new DataTable();
                        DataTable dtnew = new DataTable();
                        DataTable dtt = null;
                        string ls_guid = null;
                        switch (FunctionId)
                        {
                            #region 执行动态语句     0002
                            case "0002":
                                ls_columnname = new string[1];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "sql";
                                dt = GetAllColumnsData(ls_columnname, 0);
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "0":       /*查询、当前只支持单SQL查询*/
                                        ls_returnmsg = FindDataTable(dt, out ls_returndt);
                                        break;
                                    case "1":       /*插入*/
                                        ls_returnmsg = SqlDataTable(dt);
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = SqlDataTable(dt);
                                        break;
                                    case "3":       /*删除*/
                                        ls_returnmsg = SqlDataTable(dt);
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传机构商品目录    2002
                            case "2002": /*上传机构商品目录 */
                                ls_columnname = new string[33];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "WAREGUID".ToLower();
                                ls_columnname[1] = "SPECS".ToLower();
                                ls_columnname[2] = "DRUGGUID".ToLower();
                                ls_columnname[3] = "UNITGUID".ToLower();
                                ls_columnname[4] = "PRODUCTFACTGUID".ToLower();
                                ls_columnname[5] = "PRODUCTAREAGUID".ToLower();
                                ls_columnname[6] = "TYPEGUID".ToLower();
                                ls_columnname[7] = "KINDGUID".ToLower();
                                ls_columnname[8] = "FORMGUID".ToLower();
                                ls_columnname[9] = "SAVEKINDGUID".ToLower();
                                ls_columnname[10] = "WARECODE".ToLower();
                                ls_columnname[11] = "WARENAME".ToLower();
                                ls_columnname[12] = "ONAME".ToLower();
                                ls_columnname[13] = "BAR".ToLower();
                                ls_columnname[14] = "SAVECONDIT".ToLower();
                                ls_columnname[15] = "PACKNUM".ToLower();
                                ls_columnname[16] = "PACKWEIGHT".ToLower();
                                ls_columnname[17] = "PACKDEPTH".ToLower();
                                ls_columnname[18] = "PACKHEIGHT".ToLower();
                                ls_columnname[19] = "PACKWIDTH".ToLower();
                                ls_columnname[20] = "PACKVOLUMN".ToLower();
                                ls_columnname[21] = "SINGLEWEIGHT".ToLower();
                                ls_columnname[22] = "SINGLEWIDTH".ToLower();
                                ls_columnname[23] = "SINGLEHEIGHT".ToLower();
                                ls_columnname[24] = "SINGLEDEPTH".ToLower();
                                ls_columnname[25] = "SINGLEVOLUMN".ToLower();
                                ls_columnname[26] = "MIDPACK".ToLower();
                                ls_columnname[27] = "QUALSTAND".ToLower();
                                ls_columnname[28] = "APPROVALNUM".ToLower();
                                ls_columnname[29] = "QUALDATE".ToLower();
                                ls_columnname[30] = "REMARK".ToLower();
                                ls_columnname[31] = "PYM".ToLower();
                                ls_columnname[32] = "WBM".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["wareguid"].ToString()))
                                        {
                                            dr["wareguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["wareguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select wareguid from tb_wms_wmsware 
                                    where warecode = '" + dr["warecode"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["wareguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入机构商品目录 */
                                        ls_returnmsg = data_base_do(dt, FunctionId, ls_dbtype, out dtnew);
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        dt = dtnew;
                                        ls_returnmsg = InsertDataTable(dt, "tb_wms_wmsware", "");
                                        break;
                                    case "2":       /*更新机构商品目录 */
                                        ls_returnmsg = data_base_do(dt, FunctionId, ls_dbtype, out dtnew);
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        dt = dtnew;
                                        ls_returnmsg = UpdateDataTable(dt, "tb_wms_wmsware");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 删除机构商品目录    2004
                            case "2004":            /*删除机构商品目录 */
                                ls_columnname = new string[1];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "wareguid";
                                dt = GetAllColumnsData(ls_columnname, 0);
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = DeleteDataTable(dt, "tb_wms_wmsware");
                                break;
                            #endregion
                            #region 查询机构商品目录    2005
                            case "2005":            /*查询机构商品目录 */
                                ReadXml(uploadDataXML);
                                dt = GetAllColumnsData();
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = FindDataTable(dt, FunctionId, ls_dbtype, out ls_returndt);
                                break;
                            #endregion
                            #region 上传采购入库订单    2006
                            case "2006": /*上传采购入库订单*/
                                ls_columnname = new string[20];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORDERGUID".ToLower();
                                ls_columnname[1] = "ORGGUID".ToLower();
                                ls_columnname[2] = "COMPANYGUID".ToLower();
                                ls_columnname[3] = "WMSCOMPANYGUID".ToLower();
                                ls_columnname[4] = "OLDORDERGUID".ToLower();
                                ls_columnname[5] = "ORDERTYPE".ToLower();
                                ls_columnname[6] = "ORDERNO".ToLower();
                                ls_columnname[7] = "WARESUM".ToLower();
                                ls_columnname[8] = "SINGLESUM".ToLower();
                                ls_columnname[9] = "PACKSUM".ToLower();
                                ls_columnname[10] = "OPERUSER".ToLower();
                                ls_columnname[11] = "OPERDATE".ToLower();
                                ls_columnname[12] = "REMARK".ToLower();
                                ls_columnname[13] = "STATUS".ToLower();
                                ls_columnname[14] = "GOUSER".ToLower();
                                ls_columnname[15] = "GODATE".ToLower();
                                ls_columnname[16] = "GETUSER".ToLower();
                                ls_columnname[17] = "GETDATE".ToLower();
                                ls_columnname[18] = "CHECKUSER".ToLower();
                                ls_columnname[19] = "CHECKDATE".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dr["orderguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select orderguid from Tb_Wms_BugInstoreOrder 
                                    where orderno = '" + dr["orderno"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["orderguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_BugInstoreOrder", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_BugInstoreOrder");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_BugInstoreOrder", "orderguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传采购入库订单明细  2007
                            case "2007": /*上传采购入库订单明细*/
                                ls_columnname = new string[13];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORDERDETAILGUID".ToLower();
                                ls_columnname[1] = "ORDERGUID".ToLower();
                                ls_columnname[2] = "WAREGUID".ToLower();
                                ls_columnname[3] = "BUSINESSWAREGUID".ToLower();
                                ls_columnname[4] = "WARECODE".ToLower();
                                ls_columnname[5] = "WARENAME".ToLower();
                                ls_columnname[6] = "WAREBAR".ToLower();
                                ls_columnname[7] = "SPECS".ToLower();
                                ls_columnname[8] = "FACTAREA".ToLower();
                                ls_columnname[9] = "UNITS".ToLower();
                                ls_columnname[10] = "SINGLENUM".ToLower();
                                ls_columnname[11] = "PACKNUM".ToLower();
                                ls_columnname[12] = "REMARK".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["ORDERDETAILGUID"].ToString()))
                                        {
                                            dr["ORDERDETAILGUID"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["ORDERDETAILGUID"].ToString()))
                                        {
                                            ls_returnmsg = "未传输订单明细GUID，请检查";
                                            break;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_BugInstoreOrderDetail", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_BugInstoreOrderDetail");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_BugInstoreOrderDetail", "orderdetailguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传销售退货入库订单  2008
                            case "2008": /*上传销售退货入库订单*/
                                ls_columnname = new string[21];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORDERGUID".ToLower();
                                ls_columnname[1] = "SALEOUTSTOREORDERGUID".ToLower();
                                ls_columnname[2] = "ORGGUID".ToLower();
                                ls_columnname[3] = "BUSINESSCOMPANYGUID".ToLower();
                                ls_columnname[4] = "WMSCOMPANYGUID".ToLower();
                                ls_columnname[5] = "ORDERNO".ToLower();
                                ls_columnname[6] = "WARESUM".ToLower();
                                ls_columnname[7] = "PACKSUM".ToLower();
                                ls_columnname[8] = "SINGLESUM".ToLower();
                                ls_columnname[9] = "OPERUSER".ToLower();
                                ls_columnname[10] = "OPERDATE".ToLower();
                                ls_columnname[11] = "STATUS".ToLower();
                                ls_columnname[12] = "REMARK".ToLower();
                                ls_columnname[13] = "GOUSER".ToLower();
                                ls_columnname[14] = "GODATE".ToLower();
                                ls_columnname[15] = "GETUSER".ToLower();
                                ls_columnname[16] = "GETDATE".ToLower();
                                ls_columnname[17] = "CHECKUSER".ToLower();
                                ls_columnname[18] = "CHECKDATE".ToLower();
                                ls_columnname[19] = "ORDERTYPE".ToLower();
                                ls_columnname[20] = "OLDORDERGUID".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dr["orderguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select orderguid from Tb_Wms_SaleReturnInstoreOrder 
                                    where orderno = '" + dr["orderno"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["orderguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_SaleReturnInstoreOrder", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_SaleReturnInstoreOrder");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_SaleReturnInstoreOrder", "orderguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传销售退货入库订单明细    2009
                            case "2009": /*上传销售退货入库订单明细*/
                                ls_columnname = new string[15];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORDERDGUID".ToLower();
                                ls_columnname[1] = "ORDERGUID".ToLower();
                                ls_columnname[2] = "BUSINESSWAREGUID".ToLower();
                                ls_columnname[3] = "WAREGUID".ToLower();
                                ls_columnname[4] = "WARECODE".ToLower();
                                ls_columnname[5] = "WARENAME".ToLower();
                                ls_columnname[6] = "SPECS".ToLower();
                                ls_columnname[7] = "FACTAREA".ToLower();
                                ls_columnname[8] = "UNITS".ToLower();
                                ls_columnname[9] = "SINGLENUM".ToLower();
                                ls_columnname[10] = "PACKNUM".ToLower();
                                ls_columnname[11] = "REMARK".ToLower();
                                ls_columnname[12] = "BATCHS".ToLower();
                                ls_columnname[13] = "EXPDATE".ToLower();
                                ls_columnname[14] = "PRODUCTDATE".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["ORDERDGUID"].ToString()))
                                        {
                                            dr["ORDERDGUID"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["ORDERDGUID"].ToString()))
                                        {
                                            ls_returnmsg = "未传输订单明细GUID，请检查";
                                            break;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_SaleReturnInstoreOrderD", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_SaleReturnInstoreOrderD");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_SaleReturnInstoreOrderD", "orderdguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传销售出库订单    2010
                            case "2010": /*上传销售出库订单*/
                                ls_columnname = new string[13];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORDERGUID".ToLower();
                                ls_columnname[1] = "ORGGUID".ToLower();
                                ls_columnname[2] = "COMPANYGUID".ToLower();
                                ls_columnname[3] = "WMSCOMPANYGUID".ToLower();
                                ls_columnname[4] = "ORDERTYPE".ToLower();
                                ls_columnname[5] = "ORDERCODE".ToLower();
                                ls_columnname[6] = "ORDERNAME".ToLower();
                                ls_columnname[7] = "SINGLESUM".ToLower();
                                ls_columnname[8] = "PACKSUM".ToLower();
                                ls_columnname[9] = "OPERUSER".ToLower();
                                ls_columnname[10] = "OPERDATE".ToLower();
                                ls_columnname[11] = "REMARK".ToLower();
                                ls_columnname[12] = "STATUS".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dr["orderguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orderguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select orderguid from Tb_Wms_SaleOutstoreOrder 
                                    where ORDERCODE = '" + dr["ORDERCODE"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["orderguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_SaleOutstoreOrder", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_SaleOutstoreOrder");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_SaleOutstoreOrder", "orderguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传销售出库订单明细  2011
                            case "2011": /*上传销售出库订单明细*/
                                ls_columnname = new string[15];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "DETAILGUID".ToLower();
                                ls_columnname[1] = "ORDERGUID".ToLower();
                                ls_columnname[2] = "BUSINESSWAREGUID".ToLower();
                                ls_columnname[3] = "WAREGUID".ToLower();
                                ls_columnname[4] = "WARECODE".ToLower();
                                ls_columnname[5] = "WARENAME".ToLower();
                                ls_columnname[6] = "SPECS".ToLower();
                                ls_columnname[7] = "FACTAREA".ToLower();
                                ls_columnname[8] = "UNITS".ToLower();
                                ls_columnname[9] = "BATCHS".ToLower();
                                ls_columnname[10] = "PRODUCTDATE".ToLower();
                                ls_columnname[11] = "EXPDATE".ToLower();
                                ls_columnname[12] = "SINGLENUM".ToLower();
                                ls_columnname[13] = "PACKNUM".ToLower();
                                ls_columnname[14] = "REMARK".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            dr["DETAILGUID"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            ls_returnmsg = "未传输订单明细GUID，请检查";
                                            break;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = data_base_do(dt, FunctionId, ls_dbtype, out dtnew);
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        dt = dtnew;
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_SaleOutstoreOrderDetail", "");
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        ls_orderguid = dt.Rows[0]["ORDERGUID"].ToString();
                                        ls_orgguid = GetDataTable(@"select orgguid from Tb_Wms_SaleOutstoreOrder 
                                    where orderguid = '" + ls_orderguid + "'").Rows[0][0].ToString();
                                        for (int i = 0; i < dt.Rows.Count; i++)
                                        {
                                            ls_guid = GetDataTable("select createguid() from dual").Rows[0][0].ToString();
                                            ls_returnmsg = SqlDataTable(@"INSERT INTO TB_WMS_ORDERKEEPSTORE(KEEPGUID,ORDERGUID,ORGGUID,WAREGUID,BATCHS,KEEPSINGLENUM,KEEPNUM,REMARK) VALUES('" + ls_guid + "','" + ls_orderguid + "','" + ls_orgguid + "','" + dt.Rows[i]["WAREGUID"].ToString() + "','" + dt.Rows[i]["BATCHS"].ToString() + "'," + dt.Rows[i]["SINGLENUM"].ToString() + "," + dt.Rows[i]["PACKNUM"].ToString() + ",NULL)");
                                            if (ls_returnmsg != "TRUE")
                                            {
                                                break;
                                            }
                                        }
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_SaleOutstoreOrderDetail");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_SaleOutstoreOrderDetail", "detailguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传购进退货出库订单    2012
                            case "2012": /*购进退货出库订单*/
                                ls_columnname = new string[13];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "DETAILGUID".ToLower();
                                ls_columnname[1] = "ORDERGUID".ToLower();
                                ls_columnname[2] = "ORGGUID".ToLower();
                                ls_columnname[3] = "COMPANYGUID".ToLower();
                                ls_columnname[4] = "WMSCOMPANYGUID".ToLower();
                                ls_columnname[5] = "ORDERCODE".ToLower();
                                ls_columnname[6] = "ORDERNAME".ToLower();
                                ls_columnname[7] = "SINGLESUM".ToLower();
                                ls_columnname[8] = "PACKSUM".ToLower();
                                ls_columnname[9] = "OPERUSER".ToLower();
                                ls_columnname[10] = "OPERDATE".ToLower();
                                ls_columnname[11] = "REMARK".ToLower();
                                ls_columnname[12] = "STATUS".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            dr["DETAILGUID"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select DETAILGUID from Tb_Wms_BugReturnOutstoreOrder 
                                    where ORDERCODE = '" + dr["ORDERCODE"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["orderguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_BugReturnOutstoreOrder", "");
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_BugReturnOutstoreOrder");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_BugReturnOutstoreOrder", "detailguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传购进退货出库订单明细  2013
                            case "2013": /*购进退货出库订单明细*/
                                ls_columnname = new string[15];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "DETAILGUID".ToLower();
                                ls_columnname[1] = "ORDERGUID".ToLower();
                                ls_columnname[2] = "BUSINESSWAREGUID".ToLower();
                                ls_columnname[3] = "WAREGUID".ToLower();
                                ls_columnname[4] = "WARENO".ToLower();
                                ls_columnname[5] = "WARENAME".ToLower();
                                ls_columnname[6] = "SPECS".ToLower();
                                ls_columnname[7] = "FACTAREA".ToLower();
                                ls_columnname[8] = "UNITS".ToLower();
                                ls_columnname[9] = "BATCHS".ToLower();
                                ls_columnname[10] = "PRODUCTDATE".ToLower();
                                ls_columnname[11] = "EXPDATE".ToLower();
                                ls_columnname[12] = "SINGLENUM".ToLower();
                                ls_columnname[13] = "PACKNUM".ToLower();
                                ls_columnname[14] = "REMARK".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            dr["DETAILGUID"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["DETAILGUID"].ToString()))
                                        {
                                            ls_returnmsg = "未传输订单明细GUID，请检查";
                                            break;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入*/
                                        ls_returnmsg = data_base_do(dt, FunctionId, ls_dbtype, out dtnew);
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        dt = dtnew;
                                        ls_returnmsg = InsertDataTable(dt, "Tb_Wms_BugReturnOutstoreOrderD", "");
                                        if (ls_returnmsg != "TRUE")
                                        {
                                            break;
                                        }
                                        ls_orderguid = dt.Rows[0]["ORDERGUID"].ToString();
                                        ls_orgguid = GetDataTable(@"select orgguid from Tb_Wms_BugReturnOutstoreOrder 
                                    where orderguid = '" + ls_orderguid + "'").Rows[0][0].ToString();
                                        for (int i = 0; i < dt.Rows.Count; i++)
                                        {
                                            ls_guid = GetDataTable("select createguid() from dual").Rows[0][0].ToString();
                                            ls_returnmsg = SqlDataTable(@"INSERT INTO TB_WMS_ORDERKEEPSTORE(KEEPGUID,ORDERGUID,ORGGUID,WAREGUID,BATCHS,KEEPSINGLENUM,KEEPNUM,REMARK) VALUES('" + ls_guid + "','" + ls_orderguid + "','" + ls_orgguid + "','" + dt.Rows[i]["WAREGUID"].ToString() + "','" + dt.Rows[i]["BATCHS"].ToString() + "'," + dt.Rows[i]["SINGLENUM"].ToString() + "," + dt.Rows[i]["PACKNUM"].ToString() + ",NULL)");
                                            if (ls_returnmsg != "TRUE")
                                            {
                                                break;
                                            }
                                        }
                                        break;
                                    case "2":       /*更新*/
                                        ls_returnmsg = UpdateDataTable(dt, "Tb_Wms_BugReturnOutstoreOrderD");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "Tb_Wms_BugReturnOutstoreOrderD", "detailguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传机构目录  2014
                            case "2014": /*上传机构目录 */
                                ls_columnname = new string[12];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "ORGGUID".ToLower();
                                ls_columnname[1] = "NATUREGUID".ToLower();
                                ls_columnname[2] = "LEVELGUID".ToLower();
                                ls_columnname[3] = "ORGCODE".ToLower();
                                ls_columnname[4] = "ORGNAME".ToLower();
                                ls_columnname[5] = "ADDRESS".ToLower();
                                ls_columnname[6] = "CONTACT".ToLower();
                                ls_columnname[7] = "TEL".ToLower();
                                ls_columnname[8] = "MAIL".ToLower();
                                ls_columnname[9] = "QQ".ToLower();
                                ls_columnname[10] = "FAX".ToLower();
                                ls_columnname[11] = "REMARK".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orgguid"].ToString()))
                                        {
                                            dr["orgguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["orgguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select orgguid from tb_wms_org
                                    where orgcode = '" + dr["orgcode"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["orgguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入机构目录 */
                                        ls_returnmsg = InsertDataTable(dt, "tb_wms_org", "");
                                        break;
                                    case "2":       /*更新机构目录 */
                                        ls_returnmsg = UpdateDataTable(dt, "tb_wms_org");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "tb_wms_org", "orgguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 上传往来单位目录    2015
                            case "2015": /*上传往来单位目录 */
                                ls_columnname = new string[15];
                                ReadXml(uploadDataXML);
                                ls_columnname[0] = "COMPANYGUID".ToLower();
                                ls_columnname[1] = "AREAGUID".ToLower();
                                ls_columnname[2] = "NATUREGUID".ToLower();
                                ls_columnname[3] = "KINDGUID".ToLower();
                                ls_columnname[4] = "COMPANYCODE".ToLower();
                                ls_columnname[5] = "COMPANYNAME".ToLower();
                                ls_columnname[6] = "SNAME".ToLower();
                                ls_columnname[7] = "CONTACT".ToLower();
                                ls_columnname[8] = "TEL".ToLower();
                                ls_columnname[9] = "ADDRESS".ToLower();
                                ls_columnname[10] = "REGNO".ToLower();
                                ls_columnname[11] = "TAXNO".ToLower();
                                ls_columnname[12] = "OPERDATE".ToLower();
                                ls_columnname[13] = "PYM".ToLower();
                                ls_columnname[14] = "WBM".ToLower();
                                dt = GetAllColumnsData(ls_columnname, 0);
                                if (ls_dbtype == "1")
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["companyguid"].ToString()))
                                        {
                                            dr["companyguid"] = getguid();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        if (string.IsNullOrEmpty(dr["companyguid"].ToString()))
                                        {
                                            dtt = new DataTable();
                                            dtt = GetDataTable(@"select companyguid from tb_wms_company 
                                    where companycode = '" + dr["companycode"].ToString() + "'");
                                            ls_guid = dtt.Rows[0][0].ToString();
                                            dr["companyguid"] = ls_guid;
                                        }
                                    }
                                }
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                switch (ls_dbtype)
                                {
                                    case "1":       /*插入往来单位目录 */
                                        ls_returnmsg = InsertDataTable(dt, "tb_wms_company", "");
                                        break;
                                    case "2":       /*更新往来单位目录 */
                                        ls_returnmsg = UpdateDataTable(dt, "tb_wms_company");
                                        break;
                                    case "3":       /*插入或更新*/
                                        ls_returnmsg = InsertOrUpdateDataTable(dt, "tb_wms_company", "companyguid");
                                        break;
                                    default:
                                        ls_returnmsg = "无法识别的操作类型";
                                        break;
                                }
                                break;
                            #endregion
                            #region 查询已上架单据目录    2017
                            case "2017":            /*查询已上架单据目录 */
                                ReadXml(uploadDataXML);
                                dt = GetAllColumnsData();
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = FindDataTable(dt, FunctionId, ls_dbtype, out ls_returndt);
                                break;
                            #endregion
                            #region 查询已上架单据明细目录    2018
                            case "2018":            /*查询已上架单据明细目录 */
                                ReadXml(uploadDataXML);
                                dt = GetAllColumnsData();
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = FindDataTable(dt, FunctionId, ls_dbtype, out ls_returndt);
                                break;
                            #endregion
                            #region 查询盘赢亏单据目录    2019
                            case "2019":            /*查询盘赢亏单据目录 */
                                ReadXml(uploadDataXML);
                                dt = GetAllColumnsData();
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = FindDataTable(dt, FunctionId, ls_dbtype, out ls_returndt);
                                break;
                            #endregion
                            #region 查询盘赢亏单据明细目录   2020
                            case "2020":            /*查询盘赢亏单据明细目录 */
                                ReadXml(uploadDataXML);
                                dt = GetAllColumnsData();
                                ls_returnmsg = data_validation(dt, FunctionId, ls_dbtype);
                                if (ls_returnmsg != "TRUE")
                                {
                                    break;
                                }
                                ls_returnmsg = FindDataTable(dt, FunctionId, ls_dbtype, out ls_returndt);
                                break;
                            #endregion
                            default:
                                ls_returnmsg = "无法识别的操作类型";
                                break;
                        }
                        #region 返回
                        if (ls_returnmsg != "" && ls_returnmsg != "TRUE")
                        {
                            RollbackTrans();
                            WriteXml(FunctionId, "1000", ls_returnmsg);
                            WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "1000");
                        }
                        else
                        {
                            CommitTrans();
                            if (ls_returndt != "")
                            {
                                retDataXML = ls_returndt;
                                WriteXml(FunctionId, "0000", "处理成功");
                                WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "0000");
                            }
                            else
                            {
                                WriteXml(FunctionId, "0000", "处理成功");
                                WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "0000");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        RollbackTrans();
                        WriteXml(FunctionId, "2001", ls_returnmsg);
                        WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "2001");
                    }
                }
                else
                {
                    RollbackTrans();
                    WriteXml(FunctionId, "9100", ls_msg);
                    WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "9100");
                }
                return (doc.InnerXml);
            }
            catch (Exception ex)
            {
                RollbackTrans();
                ls_returnmsg = ex.Message.ToString();
                WriteXml(FunctionId, "1000", ls_returnmsg);
                WriteLog(OperUserID, DateTime.Now, requestParmXML, ls_returnmsg, uploadDataXML, doc.InnerXml, "BUSINESS", null, FunctionId, "1000");
                return (doc.InnerXml);
            }
        }

        [WebMethod(Description = "集团调用接口处理的方法")]
        public string GroupInterface(string requestParmXML, string uploadDataXML, out string retDataXML)
        {
            #region 变量
            string ls_msg = "";
            string ls_returnmsg = "";
            string ls_returndt = "";
            string ls_order = "";
            string ls_model = "";
            string ls_businessid = "";
            string ls_labeladdress = "";
            string ls_waresum = "";
            string ls_warelocation = "";
            string ls_string = "";
            string ls_xml;
            string ls_warenum = "";
            DataTable dt = new DataTable();
            retDataXML = "";
            #endregion
            try
            {
                ReadXml(requestParmXML);
                OrgCode = org_no();
                controlip = wms_dbtype();
                InterfaceUserID = user_no();
                InterfacePassWord = user_pass();
                OperPassWord = oper_pass();
                Opertime = oper_time();
                Interface = oper_interface();
                FunctionId = function_id();


                #region 电子标签内部处理，不需要验证、数据库连接等
                if (Interface != "MIAWMS")
                {
                    if (String2Base64(UserMd5(FunctionId + Opertime)) != Interface)
                    {
                        WriteXml("0001", "2000", "通讯密匙验证失败");
                        return (doc.InnerXml);
                    }
                }
                //if (Interface != "MIAWMS")
                //{
                if (!getcnParms(OrgCode, out ls_msg))
                {
                    WriteXml(FunctionId, "9100", ls_msg);
                    return (doc.InnerXml);
                }
                //}
                //if (Interface != "MIAWMS")
                //{
                ls_returnmsg = checkUserValid(FunctionId, OrgCode, InterfaceUserID, InterfacePassWord, OperUserID, OperPassWord, ls_msg, 0 /*业务系统传2;PDA传1*/, 0, out ls_xml);
                //}
                //else
                //{
                //    ls_returnmsg = "TRUE";
                //}
                if (ls_returnmsg != "TRUE")
                {
                    WriteXml(FunctionId, "2001", ls_returnmsg);
                    //return doc.InnerText;
                    return (doc.InnerXml);
                }
                #endregion

                switch (FunctionId)
                {
                    default:
                        break;
                }
                #region 返回结果处理
                if (ls_returnmsg != "TRUE")
                {
                    RollbackTrans();
                    WriteXml(FunctionId, "1000", ls_returnmsg);
                    WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, null, "LABEL", FunctionId, "1000");
                }
                else
                {
                    CommitTrans();
                    if (ls_returndt != "")
                    {
                        WriteXml(FunctionId, "0000", "处理成功");
                        retDataXML = ls_returndt;
                    }
                    else
                    {
                        WriteXml(FunctionId, "0000", "处理成功");
                    }
                    WriteLog(OperUserID, DateTime.Now, requestParmXML, retDataXML, uploadDataXML, doc.InnerXml, null, "LABEL", FunctionId, "0000");
                }
                return (doc.InnerXml);
                #endregion
            }
            catch (Exception ex)
            {
                RollbackTrans();
                ls_returnmsg = ex.Message.ToString();
                WriteXml(FunctionId, "1000", ls_returnmsg);
                WriteLog(OperUserID, DateTime.Now, requestParmXML, ls_returnmsg, uploadDataXML, doc.InnerXml, null, "LABEL", FunctionId, "1000");
                return (doc.InnerXml);
            }
        }
        #endregion
    }
}


