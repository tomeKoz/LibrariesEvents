using System.Diagnostics;
using System.Text.Json;

namespace LibrariesEvents
{
    public partial class Form1 : Form
    {
        private List<LibEvent> eventsFromFile;
        private List<LibEvent> currentEvents;
        private List<LibEvent> newEvents;
        private static readonly string fileName = "events.json";

        public Form1()
        {
            InitializeComponent();
        }



        private async void zapiszJakoObejrzaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            eventsFromFile.AddRange(newEvents);
            await SaveToFile();
            MessageBox.Show($"Wydarzenia zostały zapisane jako obejrzane.");
        }

        private async Task SaveToFile()
        {
            string json = JsonSerializer.Serialize(eventsFromFile);
            await File.WriteAllTextAsync("events.json", json);
        }

        private void wyjścieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void RemoveOldEvents()
        {
            eventsFromFile = eventsFromFile.Where(a => a.Date >= DateTime.Today).ToList();
            await SaveToFile();
        }

        private async void porównajZObejrzanymiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadEventsFromFile();
            RemoveOldEvents();
            await LoadCurrentEvents();

            newEvents = currentEvents?.Where(a => !eventsFromFile.Select(a => a.Url).Contains(a.Url)).ToList();
            newEvents.ForEach(a => a.Name = a.Name?.Replace("&quot;", "\""));

            if (newEvents != null && newEvents.Count < 1)
            {
                MessageBox.Show($"Brak nowych wydarzeń :(");
                dataGridView1.DataSource = null;
                return;
            }

            MessageBox.Show($"Jest {newEvents?.Count} nowych wydarzeń!");
            dataGridView1.DataSource = newEvents;
        }

        private async Task LoadEventsFromFile()
        {
            using FileStream openStream = File.OpenRead(fileName);
            eventsFromFile = await JsonSerializer.DeserializeAsync<List<LibEvent>>(openStream);
            eventsFromFile = eventsFromFile?.OrderBy(a => a?.Name)?.ToList();
        }

        private async Task LoadCurrentEvents()
        {
            Scrapper scrapper = new();
            label1.Visible = true;
            progressBar1.Visible = true;
            currentEvents = await scrapper.GetCurrentEvents();
            currentEvents = currentEvents.OrderBy(a => a.Name).ToList();
            label1.Visible = false;
            progressBar1.Visible = false;
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["Url"] = new DataGridViewLinkCell();
            }
            dataGridView1.Columns["Url"].HeaderText = "Link";
            dataGridView1.Columns["Url"].Width = 52;
            dataGridView1.Columns["Date"].HeaderText = "Data";
            dataGridView1.Columns["Date"].Width = 102;
            dataGridView1.Columns["Name"].HeaderText = "Nazwa wydarzenia";
            dataGridView1.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns["BranchNumber"].HeaderText = "nr";
            dataGridView1.Columns["BranchNumber"].Width = 33;
            dataGridView1.Columns["BranchName"].HeaderText = "Nazwa biblioteki";
            dataGridView1.Columns["BranchName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns["Type"].HeaderText = "Typ";
            dataGridView1.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                dataGridView1.Sort(dataGridView1.Columns["Name"], System.ComponentModel.ListSortDirection.Descending);
                return;
            }
            if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewLinkCell)
            {
                var urlToOpen = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                if (!string.IsNullOrEmpty(urlToOpen))
                {
                    Process myProcess = new();
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.FileName = urlToOpen;
                    myProcess.Start();
                }
            }
        }
    }
}