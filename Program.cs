// See https://aka.ms/new-console-template for more information

using System.Text;
using LibreHardwareMonitor.Hardware;
using System.Net.Sockets;
using System.Globalization;
using Newtonsoft.Json;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Management;



public class Program
{

    struct sen_entry
    {

        [Name("sensor")]
        public string nome { get; set; }

        [Name("type")]
        public SensorType type { get; set; }


    }
    public static void Main(string[] args)
    {
        
        int sleep_total = 5000;
        string server_ip = "200.235.173.107";
        //string server_ip = "192.168.1.232";
        Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
            
        };
        
        bool ip_found = true;
        

        if(args.Length > 0)
        {

            int arg_indx = 0;
            bool arg_found = false;
            Console.WriteLine(args.Length);
            while(arg_indx < args.Length)
            {
                arg_found = false;
                if(args[arg_indx]=="-h")
                {
                   Console.WriteLine("-----------AJUDA------------");
                   Console.WriteLine("----------------------------");
                   return;
                }

                if(args[arg_indx]=="-t" && arg_indx+1 < args.Length)
                {
                       sleep_total = Int32.Parse(args[1]);
                       arg_found=true;
                       arg_indx+=2;
                }

                if(!arg_found && args[arg_indx]=="-ip" && arg_indx+1 < args.Length)
                {

                       
                       server_ip = args[1];
                       arg_found=true;
                       arg_indx+=2;
                       ip_found = true;
                }

                if(!arg_found)
                {
                    break;
                }
            }
        }
          
        if(!ip_found)
        {
            Console.WriteLine("Erro: Endereço do servidor não fornecido, forneça-o com o argumento -ip *Endereço do servidor*");
            return;
        }

        if(!Directory.Exists("config"))
        {
             Directory.CreateDirectory("config");
         
        }
        const Int32 BufferSize = 512;
        string config_hardware_path = "config/allowed_hardware.txt";
        string config_sensors_path = "config/allowed_sensors.txt";
        string config_sensors_type_path = "config/allowed_sensors_types.txt";
        List<string> allowed_hardware = new();
        List<string> allowed_sensors = new();
        List<SensorType> allowed_sensors_types = new();

        if(File.Exists(config_hardware_path))
        {
            using (var fileStream = File.OpenRead(config_hardware_path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    while((line=streamReader.ReadLine()) != null)
                    {
                          allowed_hardware.Add(line);
                    }
                }
            }
        }

        if(File.Exists(config_sensors_path))
        {
            using (var fileStream = new StreamReader(config_sensors_path))
            {
                
                using (var csv = new CsvReader(fileStream, CultureInfo.InvariantCulture))
                {
                    var raw_sensor_type = csv.GetRecords<sen_entry>();
                    

                    foreach(sen_entry rs in raw_sensor_type)
                    {
                        allowed_sensors.Add(rs.nome);
                        allowed_sensors_types.Add(rs.type);
                    }
                    
                }
                /*using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    while((line=streamReader.ReadLine()) != null)
                    {
                          allowed_sensors.Add(line);
                    }
                }*/
            }
        }

        /*if(File.Exists(config_sensors_type_path))
        {
            using (var fileStream = File.OpenRead(config_sensors_type_path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    while((line=streamReader.ReadLine()) != null)
                    {
                          int cov = Int32.Parse(line);
                          allowed_sensors_types.Add((SensorType)cov);
                    }
                }
            }
        }*/

    
        
       
        


        Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        IPAddress hostIP =IPAddress.Parse(server_ip);
        var ep = new IPEndPoint(hostIP, 3335);
        
        computer.Open();

        socket.ConnectAsync(ep);

        
        

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

        StringBuilder sb = new StringBuilder();
        int i=0;

        //string fr = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}";
        //string fr = "{0,21}";
        List<string> sensor_names = new(22);
        List<Object> val_names = new(22);

        List<string> hardware_names = new();
        List<string> h_sensor_names = new();


        int beep_tone =1500;

        sensor_names.Add("date");
        string hardware_dump_path = "all_hardware.txt";
        string sensor_dump_path = "all_sensors.txt";
        string sensor_type_dump_path = "all_sensor_types.txt";

        bool hardware_dumped=File.Exists(hardware_dump_path);
        bool sensors_dumped=File.Exists(sensor_dump_path);
        bool sensor_types_dumped=File.Exists(sensor_type_dump_path);
        byte[] trash = new byte[1024];

        foreach (IHardware hardware in computer.Hardware)
        {

            //Console.WriteLine("Hardware");
            //Console.WriteLine(hardware.Name);
            if(hardware_dumped==false)
            {
                hardware_names.Add(hardware.Name);
            }

            if(allowed_hardware.Any() && !allowed_hardware.Contains(hardware.Name))
            {
                 continue;
            }
           

            foreach (ISensor sensor in hardware.Sensors)
            {
                //Console.WriteLine(sensor.Name);
                if(sensors_dumped==false)
                {
                    StringBuilder sbd = new();
                    sbd.Append(sensor.Name);
                    sbd.Append(',');
                    sbd.Append((int)sensor.SensorType);
                    sbd.Append(',');
                    sbd.Append(sensor.SensorType);
                    h_sensor_names.Add(sbd.ToString());



                    sbd.Clear();
                }
                //if(sensor.SensorType != SensorType.Temperature){continue;}
                

                bool cond1 = !allowed_sensors.Any()||allowed_sensors.Contains(sensor.Name);
                bool cond2 = !allowed_sensors_types.Any()||allowed_sensors_types.Contains(sensor.SensorType);
                if(!cond1||!cond2)
                {
                    continue;
                }
                

                sensor_names.Add(sensor.Name);
            }
            
        }

        //Console.WriteLine(sensor_names.Count);


        if(! File.Exists("log.txt"))
        {
            Console.WriteLine(sensor_names.Count);
            using (StreamWriter outputFile = File.AppendText("log.txt"))
            {
            //Console.WriteLine(String.Join(", ",sensor_names));
            outputFile.WriteLine(String.Join(", ",sensor_names));
            }

        }

        if(hardware_dumped==false)
        {

            Console.WriteLine("Dumping hardware");
            using (StreamWriter outputFile = File.AppendText(hardware_dump_path))
            {
               //Console.WriteLine(String.Join(", ",sensor_names));
               foreach(string hn in hardware_names)
               {
                          outputFile.WriteLine(hn);
               }

              
            }

             if(!File.Exists(config_sensors_path ))
             {
                  File.Copy(hardware_dump_path, config_hardware_path  );
             }
        }

        if(sensors_dumped==false)
        {
            Console.WriteLine("Dumping sensors");
            using (StreamWriter outputFile = File.AppendText(sensor_dump_path))
            {
               //Console.WriteLine(String.Join(", ",sensor_names));
               foreach(string hn in h_sensor_names)
               {
                          outputFile.WriteLine(hn);
               }

              
            }

             if(!File.Exists(config_sensors_path ))
             {
                  File.Copy(sensor_dump_path, config_sensors_path );
             }
        }


        if(sensor_types_dumped==false)
        {

            Console.WriteLine("Dumping sensors");
            using (StreamWriter outputFile = File.AppendText(sensor_type_dump_path))
            {
               //Console.WriteLine(String.Join(", ",sensor_names));
                StringBuilder sbx = new();
                for(int q=0;q<18;q++)
                {
                   sbx.Append(((SensorType)q).ToString());
                   sbx.Append(',');
                   sbx.Append(q);

                   outputFile.WriteLine(sbx.ToString());
                   sbx.Clear();
                }



              
            }

           
        }
        

        DateTime dt = new();

        SelectQuery query = new SelectQuery("SELECT LastBootUpTime FROM Win32_OperatingSystem WHERE Primary='true'");

        ManagementObjectSearcher search = new(query);

        foreach(ManagementObject mo in search.Get())
        {
            dt = ManagementDateTimeConverter.ToDateTime(mo.Properties["LastBootUpTime"].Value.ToString());

            Console.WriteLine(dt);
        }

        while(true)
        {
                i++;
                
                try{
                    if(!socket.Connected)
                    {
                        //socket.Shutdown(SocketShutdown.Send);
                        socket.Close();
                        socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.ConnectAsync(ep);
                        /*IAsyncResult result = socket.BeginConnect( ep, null, null );
                        bool done = result.AsyncWaitHandle.WaitOne( 10000, true );
                        if(done)
                        {
                            socket.EndConnect(result);
                        }
                        else{
                            socket.Close();
                            socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        
                        }*/
                    }
                }
                catch(Exception ex)
                {
                      Console.WriteLine(ex.Message);
                      //socket.Disconnect(true);
                }
                Console.WriteLine( socket.Connected);

                computer.Open();
                using (StreamWriter outputFile = File.AppendText("log.txt"))
                {
                    
                    
                    val_names.Add(DateTimeOffset.UtcNow.ToLocalTime());
                                
                    foreach (IHardware hardware in computer.Hardware)
                    {
            
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            //if(sensor.SensorType != SensorType.Temperature){continue;}
                            if(!sensor_names.Contains(sensor.Name))
                            {
                                continue;
                            }

                            float sv = (sensor.Value == null) ? 0 : (float)sensor.Value;
                            
                            val_names.Add(sv);
                            
                        }

                    

                    }
                    Console.WriteLine("Val names count");
                    Console.WriteLine(val_names.Count);
                    //sb.AppendFormat(fr,val_names);
                    sb.AppendJoin(", ",val_names);
                    outputFile.WriteLine(sb.ToString());
                    sb.Clear();
                    
            
                        
            
            
                }
                var temp_vals = new Dictionary<string, object>();
                //var values = new Dictionary<string, string>{{ "thing1", "hello" },{ "thing2", "world" }};
                //sb.Append((float)val_names[^1]);
                temp_vals["computer_name"] = System.Environment.MachineName;
                
                String stt = DateTime.Now.ToString("HH:mm:ss");
                temp_vals["sent_time"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

                var time_diff  =DateTime.Now-dt;
                Console.WriteLine(dt);
                Console.WriteLine(time_diff);
                string format = "HH:mm:ss";
                if( time_diff.Days > 0)
                {
                      format = "HH:mm:ss";
                }
                

                temp_vals["boot_time"] = ((DateTimeOffset)dt).ToUnixTimeSeconds();
                temp_vals["boot_time_unix"] = true;
                /*temp_vals["gpu_hotspot"] = (float)val_names[^1];
                

                //sb.Append((float)val_names[^4]);
                temp_vals["cpu_core_max"] = (float)val_names[^4];*/
                
                for(int a=0;a<allowed_sensors.Count;a++)
                {
                      temp_vals[allowed_sensors[a]] = -1.0f;   
                }

                //sensor_names.Remove("Core Max");
                for(int a=0;a<sensor_names.Count;a++)
                {
                    temp_vals[sensor_names[a]] = val_names[a];
                    //Console.WriteLine(temp_vals[sensor_names[a]].GetType());
                    if( temp_vals[sensor_names[a]].GetType() == typeof(System.DateTimeOffset))
                    {
                           temp_vals[sensor_names[a]]=((System.DateTimeOffset)temp_vals[sensor_names[a]]).ToUnixTimeSeconds();
                    }
                }

                
                
                var json_dict = JsonConvert.SerializeObject(temp_vals);

                Console.WriteLine(json_dict);

                var j_bytes = Encoding.ASCII.GetBytes(json_dict);


                //var content = new FormUrlEncodedContent(temp_vals);
                //var content = JsonContent.Create(temp_vals);
                socket.SendAsync(j_bytes);
                
                


                if(socket.Connected && socket.Available > 0)
                {

                    
                    Console.Beep(beep_tone,2000);
                    Thread.Sleep(3000);
                    Console.Beep(beep_tone,2000);
                    Thread.Sleep(3000);
                    Console.Beep(beep_tone,2000);
                    

                    socket.ReceiveAsync(trash);

                }

                
                /*if(socket.Connected==true && resp.Status == TaskStatus.RanToCompletion)
                {
                    Console.WriteLine("Received answer");
                }*/

                //var response = client.PostAsync(server_ip, content);
                
                val_names.Clear();
                Console.WriteLine("Sent");
                computer.Close();
                
                Thread.Sleep(sleep_total );


                
        



        }
        
        Console.WriteLine("Done!");
   
   
   }
}


