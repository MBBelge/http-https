using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Data;
using System.Reflection;

namespace Program
{
    class Program
    {
        public static void LogYazdir(DataTable tablo)
        {
            try
            {
                File.Copy(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\log.csv", Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\log-eski.csv");
            }
            finally
            {
                var lines = new List<string>();

                string[] columnNames = tablo.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

                var header = string.Join(",", columnNames);
                lines.Add(header);

                var valueLines = tablo.AsEnumerable().Select(row => string.Join(",", row.ItemArray));
                lines.AddRange(valueLines);

                File.WriteAllLines(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\log.csv", lines);
            }
        }

        public static void Main()
        {
            DataTable log = new DataTable();
            log.Columns.Add("Dosya", typeof(string));
            log.Columns.Add("URL", typeof(string));
            log.Columns.Add("Durum", typeof(string));
            log.Columns.Add("Aciklama", typeof(string));

            //Liste ve Sözlük Tanımları
            List<string> sonucListe = new List<string>();
            List<string> degisListe = new List<string>();
            Dictionary<string, bool> kayitListe = new Dictionary<string, bool>();
            Dictionary<string, string> detayListe = new Dictionary<string, string>();
            //

            //Değişkenler
            bool kayitListeCikti = false;
            bool httpsTestSonuc = false;
            int sayacDeger = 0;
            string konum = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //

            //Dosya listesi oluşturma
            //Dosya uzantıları listesi
            var ext = new List<string> {
                ".xhtml",
                ".jrxml",
                ".js",
                ".css",
                ".svg",
                ".scss",
                ".xml"
            };
            //

            //Dosya listesi
            var dosyaListe = Directory.GetFiles(konum, "*.*", SearchOption.AllDirectories)
                .Where(s => ext.Contains(Path.GetExtension(s)));
            //
            //

            //Dosya listesindeki her kayıt için
            foreach (string dosya in dosyaListe)
            {
                sonucListe.Clear();
                degisListe.Clear();

                sayacDeger++;
                //Dosya Adı Yazdır
                Console.WriteLine(dosya.ToString() + " - " + sayacDeger + " / " + dosyaListe.Count());
                File.AppendAllText(konum + @"\log.txt", dosya.ToString() + " - " + sayacDeger + " / " + dosyaListe.Count() + Environment.NewLine);
                //

                string txt = File.ReadAllText(dosya);

                //Dosyadaki her http bağlantısı listeye eklenir
                foreach (Match item in Regex.Matches(txt, @"(http):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
                {
                    sonucListe.Add(item.Value);
                }

                //Listedeki her http bağlantısı için test yapılır
                foreach (string url in sonucListe)
                {
                    //Bağlantı Adı Yazdır
                    Console.WriteLine(url.ToString());
                    File.AppendAllText(konum + @"\log.txt", url.ToString() + Environment.NewLine);
                    //

                    //Daha önce kontrol edildiyse kontrol bölümü atlanır
                    if (kayitListe.TryGetValue((string)url, out kayitListeCikti))
                    {
                        string sonucText = null;
                        detayListe.TryGetValue(url, out sonucText);
                        if (kayitListeCikti)
                        {
                            Console.WriteLine("https Testi : Başarılı" + " - " + sonucText);
                            File.AppendAllText(konum + @"\log.txt", "https Testi : Başarılı." + " - " + sonucText + Environment.NewLine);
                            log.Rows.Add(dosya, url, "Basarili", sonucText);
                            httpsTestSonuc = true;
                        }
                        else
                        {
                            Console.WriteLine("https Testi : Başarısız. " + " - " + sonucText);
                            File.AppendAllText(konum + @"\log.txt", "https Testi : Başarısız." + " - " + sonucText + Environment.NewLine);
                            log.Rows.Add(dosya, url, "Basarisiz", sonucText);
                            httpsTestSonuc = false;
                        }
                    }

                    //Kontrol bölümü
                    else
                    {
                        //http --> https çevrimi yapılır
                        string https = url.Replace("http://", "https://");

                        //https bağlantı testi yapılır
                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(https);

                        //Test için ayarlar
                        //Bütün SSL sertifikaları kabul eden ayar
                        ServicePointManager.ServerCertificateValidationCallback += delegate
                        {
                            return true;
                        };
                        //Timeout ayarları
                        request.Timeout = 60000;
                        request.ReadWriteTimeout = 60000;
                        //Header ayarları
                        request.Headers.Set("scheme", "https");
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.84 Safari/537.36";
                        request.AllowAutoRedirect = true;
                        request.KeepAlive = false;
                        request.Method = "GET";
                        //

                        //Kontrol Fonksiyonu
                        try
                        {
                            var response = (HttpWebResponse)request.GetResponse();
                            Console.WriteLine("https Testi : Başarılı." + " - " + (int)response.StatusCode + " - " + response.StatusCode);
                            File.AppendAllText(konum + @"\log.txt", " - " + "https Testi : Başarılı" + " - " + (int)response.StatusCode + " - " + response.StatusCode + Environment.NewLine);
                            File.AppendAllText(konum + @"\basariliDegistirilen.txt", url + Environment.NewLine + "https Testi : Başarılı" + " - " + (int)response.StatusCode + " - " + response.StatusCode + Environment.NewLine + "-------------" + Environment.NewLine);
                            log.Rows.Add(dosya, url, "Basarili", (int)response.StatusCode + " - " + response.StatusCode);
                            httpsTestSonuc = true;
                            kayitListe.Add(url, true);
                            detayListe.Add(url, (int)response.StatusCode + " - " + response.StatusCode);
                            response.Close();
                        }//Bağlantı hatası durumu 
                        catch (WebException wex)
                        {
                            string hata = null;
                            //Protokol hatası kontrolü
                            if (wex.Status == WebExceptionStatus.ProtocolError)
                            {
                                var response = wex.Response as HttpWebResponse;
                                if (response != null)
                                {
                                    hata = (int)response.StatusCode + " - " + response.StatusDescription;
                                }
                                else
                                {
                                    hata = " - " + wex.Status + " - " + wex.Message;
                                }
                            }
                            else
                            {
                                hata = " - " + wex.Status + " - " + wex.Message;
                            }
                            Console.WriteLine("https Testi : Başarısız." + hata);
                            File.AppendAllText(konum + @"\log.txt", "https Testi : Başarısız. " + hata + Environment.NewLine);
                            File.AppendAllText(konum + @"\basarisizDegistirilen.txt", url + Environment.NewLine + "https Testi : Başarısız. " + hata + Environment.NewLine + "-------------" + Environment.NewLine);
                            log.Rows.Add(dosya, url, "Basarisiz", hata);
                            httpsTestSonuc = false;
                            kayitListe.Add(url, false);
                            detayListe.Add(url, wex.Status + " - " + wex.Message);
                        }
                    }
                    //iki testi de geçerse değişim listesine eklenir.
                    if (httpsTestSonuc)
                    {
                        degisListe.Add(url);
                    }

                    Console.WriteLine("-------------");
                    File.AppendAllText(konum + @"\log.txt", "-------------" + Environment.NewLine);
                    //Sonraki bağlantıya geçilir.
                }

                //Değişim listesindeki her kayıt için değişim yapılır.
                foreach (string str in degisListe)
                {
                    txt = txt.Replace(str, str.Replace("http://", "https://"));
                }
                //

                //Sonuç dosyaya yazılır.
                File.WriteAllText(dosya, txt);
                Console.WriteLine("Değiştirildi.");
                File.AppendAllText(konum + @"\log.txt", "Değiştirildi." + Environment.NewLine);
                //

                //Sonraki dosyaya geçilir.
                Console.WriteLine("Sonraki Dosya\n--------------------");
                File.AppendAllText(konum + @"\log.txt", "Sonraki Dosya" + Environment.NewLine + "--------------------" + Environment.NewLine);

                degisListe.Clear();
                sonucListe.Clear();
                //
            }

            //Değiştirilen dosyalar degistirilenDosyalar.txt log dosyasına yazılır.
            foreach (string dosya in dosyaListe)
            {
                File.AppendAllText(konum + @"\degistirilenDosyalar.txt", dosya + Environment.NewLine);
            }
            //

            //kayitListe sözlüğü ve detayListe sözlüğü ilişkilendirilir.
            foreach (KeyValuePair<string, bool> url in kayitListe)
            {
                bool kayitText = false;
                string detayText = null;
                kayitListe.TryGetValue(url.Key, out kayitText);
                detayListe.TryGetValue(url.Key, out detayText);
                File.AppendAllText(konum + @"\degistirilenUrller.txt", url.Key + " - " + kayitText + " - " + detayText + " - " + Environment.NewLine);
            }
            //

            //SON
            Console.Write("---SON--- ");
            File.AppendAllText(konum + @"\log.txt", "---SON---" + Environment.NewLine);
            File.AppendAllText(konum + @"\basariliDegistirilen.txt", "---SON---" + Environment.NewLine);
            File.AppendAllText(konum + @"\basarisizDegistirilen.txt", "---SON---" + Environment.NewLine);
            File.AppendAllText(konum + @"\degistirilenUrller.txt", "---SON---" + Environment.NewLine);
            File.AppendAllText(konum + @"\degistirilenDosyalar.txt", "---SON---" + Environment.NewLine);

            LogYazdir(log);
            Console.ReadKey(true);
            Console.ReadKey(true);
            Console.ReadKey(true);
            //
        }
    }
}