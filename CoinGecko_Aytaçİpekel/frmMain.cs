using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CoinGecko_Aytaçİpekel
{
    public partial class frmMain : Form
    {
        public tools myTools = new tools();
        public DAL myDal = new DAL();
        public string constr = "server =10.0.0.14; database = KRYPTO_DATA_YENI; uid = KRYPTO_DATA_YENI; pwd = KRYPTO_DATA_YENI1231231;";
        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Getgeckocoinid();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            button1_Click(null, null);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

            myDal.frmMain = this;

            myTools.frmMain = this;

            myTools.logWriter("Goliath Online");

            myDal.OpenSQLConnection(constr, myDal.myConnection);

            timer1.Interval = 5000;
            timer1.Start();
        }

        public void Getgeckocoinid()
        {
            myTools.logWriter(" Getgeckocoinid Başlıyor...");
            string url = "https://api.coingecko.com/api/v3/coins/list";
            string sonuc = myTools.WebRequestIste(url);

            if (sonuc.Contains("id"))
            {
                dynamic jObj = JsonConvert.DeserializeObject(sonuc);

                foreach (var veri in jObj)
                {
                    int kontrol = 0;
                    string koinid = veri.id;
                    string koinsembol = veri.symbol;
                    string sql = "select id, kisaltma from para where kisaltma != 'USD' and (kaynak = 'CoinGecko' or kaynak is null) and kisaltma = '" + koinsembol.ToUpper() + "'";
                    SqlDataReader oku = myDal.CommandExecuteSQLReader(sql, myDal.myConnection);
                    while (oku.Read())
                    {
                        kontrol = Convert.ToInt32(oku[0]);
                    }
                    if (kontrol > 0)
                    {
                        Getcointarih(koinid, kontrol, koinsembol);
                    }

                }
            }

        }
        public DateTime EpochToDatetime(double gelentarih)
        {

            DateTime sonuc = new DateTime(1970, 1, 1).AddMilliseconds(gelentarih);
            return sonuc;

        }
        public void Getcointarih(string koinid, int id, string sembol)
        {

            myTools.logWriter(sembol + " İçin Getcointarih Başlıyor...");
            Application.DoEvents();

            string url = "https://www.coingecko.com/price_charts/" + koinid + "/usd/max.json";
            string sonuc = myTools.WebRequestIste(url);

            if (sonuc.Contains("stats"))
            {

                bool insert = false;
                DateTime son_data_tarihi = Convert.ToDateTime("01/01/1900");

                string sql3 = "select top 1 tarih from DATA_GUNLUK_PARA (nolock) where para_id = " + id.ToString() + " order by tarih desc";
                SqlDataReader oku = myDal.CommandExecuteSQLReader(sql3, myDal.myConnection);
                while (oku.Read())
                {
                    son_data_tarihi = Convert.ToDateTime(oku[0]);
                }

                dynamic jObj = JsonConvert.DeserializeObject(sonuc);
                foreach (var veri in jObj.stats)
                {

                    string coindeger = veri[1];
                    if (veri[0] != null && coindeger != null)
                    {
                        DateTime tarih = EpochToDatetime(Convert.ToDouble(veri[0]));
                        string tarih2 = tarih.ToString("yyyy-MM-ddT00:00:00.0000000K");

                        if (son_data_tarihi == Convert.ToDateTime("01/01/1900"))
                        {
                            string sql = "insert into DATA_GUNLUK_PARA (tarih,para_id,fiyat) values" +
                                " ('" + tarih2.Replace("-", "").Substring(0, 8) + "','" + id + "','" + coindeger + "')";
                            myDal.CommandExecuteNonQuery(sql, myDal.myConnection);
                        }
                        else
                        {

                            if (Convert.ToDateTime(tarih2) >= son_data_tarihi)
                            {
                                if (insert == false)
                                {
                                    string sql4 = "UPDATE DATA_GUNLUK_PARA SET fiyat='" + coindeger + "' WHERE " +
                                        "tarih ='" + tarih2.Replace("-", "").Substring(0, 8) + "' and para_id=" + id;
                                    myDal.CommandExecuteNonQuery(sql4, myDal.myConnection);
                                    insert = true;
                                }
                                else
                                {

                                    string sql = "insert into DATA_GUNLUK_PARA (tarih,para_id,fiyat) values " +
                                        "('" + tarih2.Replace("-", "").Substring(0, 8) + "','" + id + "','" + coindeger + "')";
                                    myDal.CommandExecuteNonQuery(sql, myDal.myConnection);

                                }

                            }
                        }

                    }


                }
                if (insert == false)
                {
                    myTools.logWriter("Yeni coin verileri çekiliyor.");
                }
                else
                {
                    myTools.logWriter("Eski coin verileri güncelleriniyor.");
                }
                Application.DoEvents();
                string sql2 = "UPDATE PARA SET cekildi= 1 , kaynak='CoinGecko' WHERE id=" + id;
                myDal.CommandExecuteNonQuery(sql2, myDal.myConnection);
                myTools.logWriter(sembol + " Verileri çekildi.");
                Application.DoEvents();
                tools.bekle(1000);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();


        }
    }
}
