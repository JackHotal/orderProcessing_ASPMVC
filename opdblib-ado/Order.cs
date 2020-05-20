using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace opdblib_ado
{
    public class Order
    {
        #region Data

        public SqlConnection mcn;
        private const string mdbname = "Orders";
        #endregion

        //Data Source=(local);Initial Catalog=Orders;Integrated Security=True
        ///Data Source=(local);Initial Catalog=Orders;User ID=ism6236;Password=***********
        //Data Source=(local);Initial Catalog=Orders;Integrated Security=True

        #region Constructor-Destructor

        public Order(string uid, string pass)
        {
            mcn = new SqlConnection();
            //mcn.ConnectionString = String.Format("Data Source=(local);Initial Catalog={0};Persist Security Info=True;User ID={1};Password={2};", mdbname, uid, pass);
            mcn.ConnectionString = String.Format("Data Source = (local); Initial Catalog = Orders; Integrated Security = True;");
            mcn.Open();

        }


     
        ~Order()
        {
            //try
            //{
            //    if (mcn.State == ConnectionState.Open)
            //        mcn.Close();

            //}
            //catch (Exception e) { Console.WriteLine(e.Message); }
        }
        #endregion

        #region GeneralPurposeDBMethods


        public DataSet RunSelect(SqlConnection cnn, String query)

        {
            DataSet ds = new DataSet();
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand(query, cnn);

                // Set the command to run
                da.SelectCommand = cmd;
                //fill the data set
                da.Fill(ds);

                //return the dataset
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            return ds;
        }

        public int TransactSQL(String[] sql, SqlConnection cnn)


        {

            SqlCommand cmd = new SqlCommand();
            SqlTransaction trans = cnn.BeginTransaction();
            int n = 0;
            cmd.Connection = cnn;
            try
            {
                // Assign transaction object for a pending local transaction
                cmd.Transaction = trans;

                for (int i = 0; i < sql.Length; i++)
                {
                    cmd.CommandText = sql[i];
                    n += cmd.ExecuteNonQuery();
                }
                //Commit the transaction
                trans.Commit();
            }
            catch (InvalidOperationException ex)
            {
                //MessageBox.Show(ex.Message); 
                trans.Rollback();
                throw ex;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                trans.Rollback();
                throw ex;
            }

            return n;
        }
        #endregion

        #region Business Methods
        public String getProductDetail(String id)
        {
            String p = "";
            try
            {
                String sql = String.Format("Select description,price,onhand from Product where pid = '{0}'", id);
                SqlCommand s = new SqlCommand(sql, mcn);

                SqlDataReader rs = s.ExecuteReader();
                
                if (rs.HasRows)
                    while (rs.Read())
                    {

                        p = String.Format("{0},{1},{2}", rs.GetString(0), rs.GetFloat(1), rs.GetInt16(2));

                    }
                rs.Close();
            }
            catch (Exception sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
            return p;
        }

        public List<String> getProductIds()
        {
            List<String> rval = new List<String>();
            try
            {

                String sql = "Select pid from Product";
                SqlCommand s = new SqlCommand(sql, mcn);
                SqlDataReader rs = s.ExecuteReader();
                if (rs.HasRows)
                    while (rs.Read())
                    {
                        rval.Add(rs.GetString(0));
                    }
                rs.Close();
            }
            catch (Exception sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
            return rval;
        }

        public String getCustomer(String id)
        {
            String name = "";
            try
            {
                String sql = string.Format("Select name from Customer where cid = {0}", id);
                SqlCommand s = new SqlCommand(sql, mcn);

                SqlDataReader rs = s.ExecuteReader();
                if (rs.HasRows)
                    while (rs.Read())
                    {

                        name = rs.GetString(0);

                    }
                rs.Close();
            }
            catch (Exception sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
            return name;
        }

        public List<String> getCustomerOrders(String id)
        {

            List<String> rval = new List<String>();
            try
            {

                String sql = String.Format("Select DISTINCT o.oid from [Orders] o inner join OrderDetails od ON o.Oid = od.Oid WHERE o.cid = {0}", id);
                DataSet ds = RunSelect(mcn, sql);
                DataTable dt = ds.Tables[0];
                int n = dt.Rows.Count;

                for (int i = 0; i < n; i++)
                {
                    short oid = (short)dt.Rows[i][0];

                    rval.Add(String.Format("{0}", oid));
                }

            }
            catch (Exception sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
            return rval;
        }


        public List<String> getOrderDetails(String oid)
        {
            List<String> rval = new List<String>();
            try
            {
                String sql = String.Format("Select c.pid,c.quantity,c.price from [Orders] b INNER JOIN OrderDetails c ON b.oid=c.oid where b.oid ={0}", oid);
                SqlCommand s = new SqlCommand(sql, mcn);
                SqlDataReader rs = s.ExecuteReader();
                if (rs.HasRows)
                    while (rs.Read())
                    {

                        String pid = rs.GetString(0);
                        int quantity = rs.GetInt16(1);
                        float price = rs.GetFloat(2);
                        String buf = String.Format("{0},{1},{2}", pid, quantity, price);
                        rval.Add(buf);
                    }

                rs.Close();
            }
            catch (Exception sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
            return rval;
        }

        public int Purchase(String cid, List<String> od)
        {
            int rval = 0;


            try
            {
                #region CREATE A NEW ORDER ID 
                String sql = "SELECT Max(oid) FROM Orders";
                SqlCommand s = new SqlCommand(sql, mcn);
                SqlDataReader rs = s.ExecuteReader();
                int oid=0;
                if (rs.HasRows)
                    while (rs.Read())
                    {
                        oid = rs.GetInt16(0) + 1;
                    }
                rs.Close();

                #endregion

                #region CREATE A NEW ORDER QUERY
                //GregorianCalendar now = new GregorianCalendar();
                DateTime now = DateTime.Today;


                String ts = now.ToShortDateString();
                //Normally I would have combined this SQL query with the set of code below since they are
                // all update queries. I separated this to show you how to use PreparedStatements with
                // parameter queries.
                int nt = 2 * od.Count + 1;
                String [] trans = new String[nt];
                trans[0] = String.Format("Insert Into Orders(oid,[Date],cid) Values ({0}, '{1}', {2});", oid, ts, cid);

                #endregion
             
                #region CREATE ORDER DETAILS/UPDATE ONHAND QUERIES
                for (int i = 0; i < nt - 1; i += 2)
                {

                    String[] vals = od[i / 2].Split(',');
                    trans[i + 1] = String.Format("Insert Into OrderDetails(oid,pid,Quantity,Price) Values ({0}, '{1}', {2},{3});", oid, vals[0], vals[3], vals[2]);
                    trans[i + 2] = String.Format("UPDATE Product SET onhand = onhand-{0} WHERE pid = '{1}';", vals[3], vals[0]);

                }

                #endregion

                rval = TransactSQL(trans,mcn);

             
            }
            catch (Exception ex) { rval = -1; }
            return rval;
        }

        #endregion


    }
}
