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

        private async void button1_Click(object sender, EventArgs e)
        {
            Scrapper scrapper = new();
            var currentEvents = await scrapper.GetCurrentEvents();
            dataGridView1.DataSource = currentEvents;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string fileName = "events.json";
            using FileStream openStream = File.OpenRead(fileName);
            var currentEvents = await JsonSerializer.DeserializeAsync<List<LibEvent>>(openStream);
            dataGridView1.DataSource = currentEvents;
        }

        private async void zapiszJakoObejrzaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            eventsFromFile.AddRange(newEvents);
            string json = JsonSerializer.Serialize(eventsFromFile);
            await File.WriteAllTextAsync("events.json", json);
        }

        private void wyjścieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void załadujZPlikuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadEventsFromFile();
            dataGridView1.DataSource = eventsFromFile;
        }

        private async void wczytajWydarzeniaZeStronyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadCurrentEvents();
            MessageBox.Show($"Na stronie jest obecnie {currentEvents.Count} wydarzeń");
        }

        private async void porównajZObejrzanymiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentEvents == null || currentEvents.Count < 1)
            {
                await LoadCurrentEvents();
            }

            if (eventsFromFile == null || eventsFromFile.Count < 1)
            {
                await LoadEventsFromFile();
            }

            newEvents = currentEvents?.Where(a => !eventsFromFile.Select(a => a.Url).Contains(a.Url)).ToList();

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
            currentEvents = await scrapper.GetCurrentEvents();
            currentEvents = currentEvents.OrderBy(a => a.Name).ToList();
            dataGridView1.DataSource = currentEvents;
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
            dataGridView1.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns["BranchNumber"].HeaderText = "nr";
            dataGridView1.Columns["BranchNumber"].Width = 33;
            dataGridView1.Columns["BranchName"].HeaderText = "Nazwa biblioteki";
            dataGridView1.Columns["BranchName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns["Type"].HeaderText = "Typ";
            dataGridView1.Columns["Type"].Width = 70;
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