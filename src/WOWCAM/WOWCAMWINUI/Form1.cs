using WOWCAM;

namespace WOWCAMWINUI
{
    public partial class Form1 : Form
    {
        private HttpClient httpClient = new();
        private CancellationTokenSource cts = new();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = 100;
                progressBar1.Value = 0;


                var countOfAddons = 0;
                if (button1.Text == "Start")
                {
                    button1.Text = "Cancel";
                    label1.Text = "Preflight...";
                    var wowcam = new Wowcam(httpClient);
                    var sheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    var result = await wowcam.RunAsync(new Progress<IEnumerable<string>>(addonNames =>
                    {
                        label1.Text = $"Processing {addonNames.Count()} addons ...";
                        countOfAddons = addonNames.Count();
                    }),
                    new Progress<byte>(b => progressBar1.Value = b), cts.Token);

                    var duration = $"{Convert.ToDouble(result.DurationInMilliseconds) / 1000:F2}";
                    var updated = $"{result.UpdatedAddons}/{countOfAddons}";
                    label1.Text = $" Finished after {duration} seconds ({updated} addons updated)";
                }
                else
                {
                    await cts.CancelAsync();
                    button1.Text = "Start";
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    label1.Text = ex.Message;
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                button1.Text = "Start";
            }
        }
    }
}
