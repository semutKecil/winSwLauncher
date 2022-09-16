using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using Timer = System.Windows.Forms.Timer;

namespace WinSWLauncher
{
    public partial class Form1 : Form
    {
        private readonly IConfiguration Configuration;
        public Form1(IConfiguration configuration)
        {
            Configuration = configuration;
            InitializeComponent();
            components = new System.ComponentModel.Container();
            var services = configuration.GetSection("services").GetChildren();
            var listSrv = services.Select(it =>
            {
                return new ServiceObj(it["display"], it["path"], it["serviceName"]);
            }).ToList();


            var y = 10;
            for (var i = 0; i < listSrv.Count; i++)
            {
                y = (i * 55) + ((i + 1) * 10);
                var sh = new ServiceHandler(listSrv[i], this.components, y);
                this.Controls.Add(sh.groupBox);
            }

            Size boxSize = new Size(435, (y + 55 + 20 + 40));

            this.Size = boxSize;
            this.MaximumSize = boxSize;
            this.MinimumSize = boxSize;
        }



        private void Form1_Load(object sender, EventArgs e)
        {

            this.Text = Configuration["title"] ?? "Default Title";
        }
    }

    class ServiceHandler
    {
        //public Container container;
        public GroupBox groupBox;
        ServiceObj srvcObj;
        int y;
        IContainer container;
        Label status = new Label();
        Button start = new Button();
        Button install = new Button();
        private STATUS state = STATUS.Unknown;

        enum STATUS { Running, Stopped, Paused, Stopping, Starting, Changing, NotInstalled, Unknown }

        private STATUS ServiceStatus()
        {

            ServiceController sc = new ServiceController(srvcObj.serviceName);
            try
            {
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        return STATUS.Running;
                    case ServiceControllerStatus.Stopped:
                        return STATUS.Stopped;
                    case ServiceControllerStatus.Paused:
                        return STATUS.Paused;
                    case ServiceControllerStatus.StopPending:
                        return STATUS.Stopping;
                    case ServiceControllerStatus.StartPending:
                        return STATUS.Starting;
                    default:
                        return STATUS.Changing;
                }
            }
            catch (Exception e)
            {

                return STATUS.NotInstalled;
            }
        }

        public ServiceHandler(ServiceObj srvcObj, System.ComponentModel.IContainer container, int y)
        {
            this.srvcObj = srvcObj;
            this.y = y;
            this.container = container;
            this.groupBox = buildServiceGroup();
        }

        GroupBox buildServiceGroup()
        {
            GroupBox gb = new GroupBox();

            gb.Text = this.srvcObj.displayName;
            gb.Location = new Point(10, this.y);
            gb.Size = new Size(400, 55);

            this.status.Text = "Status: Start";
            this.status.Location = new Point(10, 25);
            this.status.Size = new Size(150, 20);
            this.status.Font = new Font("Arial", 7);



            this.start.Text = "Start";
            this.start.Location = new Point(315, 15);
            this.start.Click += new System.EventHandler(start_Click);
            this.start.Enabled = false;
            this.start.AutoSize = true;

            this.install.Text = "Install";
            this.install.Location = new Point(235, 15);
            this.install.Click += new System.EventHandler(install_Click);
            this.install.Enabled = false;
            this.install.AutoSize = true;

            Timer timer = new System.Windows.Forms.Timer(this.container);
            timer.Interval = 500;
            timer.Tick += new System.EventHandler(timer_tick);
            timer.Start();

            gb.Controls.Add(this.status);
            gb.Controls.Add(this.install);
            gb.Controls.Add(this.start);

            return gb;
        }

        private void timer_tick(object sender, EventArgs e)
        {
            STATUS oldState = state;
            state = ServiceStatus();
            if (oldState != state)
            {
                switch (state)
                {
                    case STATUS.NotInstalled:
                        status.Text = "Status: Not Installed";
                        install.Text = "Install";
                        install.Enabled = true;
                        start.Text = "Start";
                        start.Enabled = false;
                        break;
                    case STATUS.Running:
                        status.Text = "Status: Running";
                        start.Text = "Stop";
                        start.Enabled = true;
                        install.Text = "Uninstall";
                        install.Enabled = false;
                        break;
                    case STATUS.Stopped:
                        status.Text = "Status: Stopped";
                        start.Text = "Start";
                        start.Enabled = true;
                        install.Text = "Uninstall";
                        install.Enabled = true;
                        break;
                    case STATUS.Paused:
                        status.Text = "Status: Paused";
                        start.Text = "Start";
                        start.Enabled = false;
                        install.Text = "Uninstall";
                        install.Enabled = false;
                        break;
                    case STATUS.Stopping:
                        status.Text = "Status: Stopping";
                        start.Text = "Start";
                        start.Enabled = false;
                        install.Text = "Uninstall";
                        install.Enabled = false;
                        break;
                    case STATUS.Starting:
                        status.Text = "Status: Starting";
                        start.Text = "Start";
                        start.Enabled = false;
                        install.Text = "Uninstall";
                        install.Enabled = false;
                        break;
                    default:
                        status.Text = "Status: Changing";
                        start.Text = "Start";
                        start.Enabled = false;
                        install.Text = "Uninstall";
                        install.Enabled = false;
                        break;
                }
            }

        }

        private void install_Click(object sender, EventArgs e)
        {
            new System.Threading.Thread(() =>
            {
                if (state == STATUS.NotInstalled)
                {
                    
                    var prc = RunCmd(srvcObj.readyPath + " install");
                    prc.Start();
                }
                else
                {
                    var prc = RunCmd(srvcObj.readyPath + " uninstall");
                    prc.Start();
                }
            }).Start();

        }

        private void start_Click(object sender, EventArgs e)
        {
            new System.Threading.Thread(() =>
            {
                if (state == STATUS.Stopped)
                {
                    var prc = RunCmd(srvcObj.readyPath + " start");
                    prc.Start();
                }
                else
                {
                    var prc = RunCmd(srvcObj.readyPath + " stop");
                    prc.Start();
                }
            }).Start();

        }

        public static Process RunCmd(String command, String fileName = @"C:\Windows\System32\cmd.exe")
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {

                    FileName = fileName,
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
        }
    }

    public class ServiceObj
    {
        public string displayName;
        public string path;
        public string serviceName;
        public string readyPath;

        public ServiceObj(string displayName, string path, string serviceName)
        {
            this.displayName = displayName;
            this.path = path;
            this.serviceName = serviceName;
            this.readyPath = "\"" + path.Replace("$base", Directory.GetCurrentDirectory()) + "\"";
        }
    }
}