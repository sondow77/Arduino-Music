using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using MusicXml;
using System.Windows.Controls;

namespace Arduino_Music
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        OpenFileDialog ifile = new OpenFileDialog();
        OpenFileDialog ifolder = new OpenFileDialog();
        MusicXml.Domain.Score mxml = null;
        private void Browse_file_txt_Click(object sender, RoutedEventArgs e)
        {
            //ifile.Filter = "Archivos de imágenes (*.bmp, *.jpg)|*.bmp;*.jpg|Todos los archivos (*.*)|*.*";
            ifile.Filter = "MusicXML (*.xml)|*.xml";
            if (ifile.ShowDialog() == true && ifile.FileName.EndsWith(".xml") && File.Exists(ifile.FileName))
            {
                file_txt.Text = ifile.FileName;
                LoadTracks();
            }
            else
            {
                MessageBox.Show("Error al cargar el archivo.");
            }
        }
        private void File_txt_DragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length == 1)
            {
                if (Path.GetExtension(files[0]) == ".xml")
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        e.Handled = true;
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                        e.Effects = DragDropEffects.None;
                }
            }
        }
        private void File_txt_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length == 1)
            {
                e.Handled = true;
                file_txt.Text = files[0].ToString();
                LoadTracks();
            }
        }
        void LoadTracks()
        {
            mxml = MusicXmlParser.GetScore(file_txt.Text);
            track_cbx.Items.Clear();
            foreach(var part in mxml.Parts)
            {
                track_cbx.Items.Add(part.Name);
            }
            track_cbx.SelectedIndex = 0;
            track_cbx.IsEnabled = true;
        }
        int Track_chord_count(MusicXml.Domain.Score mxml, int track)
        {
            int chord_count = 0;
            int max_chord_count = 0;
            if (mxml != null && track >= 0 && track <= mxml.Parts.Count)
            {
                var measures = mxml.Parts[track].Measures;
                foreach (var measure in measures)
                {
                    foreach (var measure_element in measure.MeasureElements)
                    {
                        if ((bool)GetPropertyVaue(measure_element.Element, "IsChordTone"))
                        {
                            chord_count += 1;
                        }
                        else
                        {
                            chord_count = 1;
                        }
                        if (chord_count > max_chord_count)
                        {
                            max_chord_count = chord_count;
                        }
                    }
                }
            }
            return max_chord_count;
        }
        private void Folder_txt_DragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length == 1)
            {
                if (File.GetAttributes(files[0]).HasFlag(FileAttributes.Directory))
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        e.Handled = true;
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                        e.Effects = DragDropEffects.None;
            }
        }
        private void Folder_txt_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length == 1)
            {
                e.Handled = true;
                folder_txt.Text = files[0].ToString();
                convert_btn.IsEnabled = true;
            }
        }
        double NoteDuration(int tempo, int metric)
        {
            return (double)60000/(tempo*metric);
        }
        double NoteFrequency(string note_in)
        {
            if (note_in == "S")
            {
                return 0;
            }
            else
            {
                string[] notes = { "C", "c", "D", "d", "E",
                "F", "f", "G", "g", "A", "a","B"};

                int note = Array.IndexOf(notes, note_in.Substring(0, 1));
                int octave = Convert.ToInt32(note_in.Substring(1, 1));
                int note_abs = octave * 12 + note;
                int dif = note_abs - 57;
                return 440 * Math.Pow(2,(double)dif/12);
            }
        }
        object GetPropertyVaue(object o, string p)
        {
            return o.GetType().GetProperty(p).GetValue(o, null);
        }
        private void Convert_btn_Click(object sender, RoutedEventArgs e)
        {
            string nl = Environment.NewLine;
            /*int measures_count = mxml.Parts[track_cbx.SelectedIndex].Measures.Count;
            string[] frequencies = new string[measures_count];
            frequencies[0] = "int nt[] = {";
            string[] durations = new string[measures_count];
            durations[0] = "int dr[] = {";
            for (int i = 1; i < measures_count;i++)
            {
                frequencies[i] = "";
                durations[i] = "";
            }*/
            string code = "";
            string frequencies = "";
            string durations = "";
            string[] freqs;
            string[] durs;
            int ten_count = 0;
            if (mxml != null)
            {
                int note_count = 0;
                foreach (var measure in mxml.Parts[track_cbx.SelectedIndex].Measures)
                {
                    foreach(var measure_element in measure.MeasureElements)
                    {
                        var element = measure_element.Element;
                        if (!(bool)GetPropertyVaue(element, "IsChordTone"))
                        {
                            
                            int dur = (int)GetPropertyVaue(element, "Duration");
                            object pitch = GetPropertyVaue(element, "Pitch");
                            int freq = 0;
                            if (pitch != null)
                            {
                                string note = GetPropertyVaue(pitch, "Step").ToString();
                                bool alter = GetPropertyVaue(pitch, "Alter").ToString() == "1";
                                string octave = GetPropertyVaue(pitch, "Octave").ToString();

                                string nota = note;
                                if (alter)
                                {
                                    nota = nota.ToLower();
                                }
                                nota += octave;
                                freq = (int)Math.Floor(NoteFrequency(nota));
                            }
                            
                            note_count += 1;
                            if(note_count != ((int)(note_count/10)*10))
                            {
                                frequencies += freq + ",";
                                durations += dur + ",";
                            }
                            else
                            {
                                frequencies += freq;
                                durations += dur;
                                ten_count += 1;
                                if(ten_count == 1)
                                {
                                    code += "int nt[] = {" + frequencies.Remove(frequencies.Length - 1, 1) + "};" + nl + "int dr[] = {" + durations.Remove(durations.Length - 1, 1) + "};"
                                        + nl + "void setup()" + nl + "{" + nl + "	for (int i = 0; i < 10; i++)"
                                        + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 0.30);"
                                        + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}" + nl;
                                }
                                else
                                {
                                    freqs = frequencies.Split(',');
                                    durs = durations.Split(',');
                                    for(int l = 0; l < freqs.Length;  l++)
                                    {
                                        code += "	nt[" + l.ToString() + "] = " + freqs[l] + ";" + nl;
                                        code += "	dr[" + l.ToString() + "] = " + durs[l] + ";" + nl;
                                    }
                                    code += "	for (int i = 0; i < " + freqs.Length + "; i++)"
                                        + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 0.30);"
                                        + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}" + nl;
                                }
                                frequencies = "";
                                durations = "";
                            }
                        }
                    }
                }
                if (ten_count > 1)
                {
                    frequencies = frequencies.Remove(frequencies.Length - 1, 1);
                    durations = durations.Remove(durations.Length - 1, 1);

                    freqs = frequencies.Split(',');
                    durs = durations.Split(',');
                    for (int l = 0; l < freqs.Length; l++)
                    {
                        code += "	nt[" + l.ToString() + "] = " + freqs[l] + ";" + nl;
                        code += "	dr[" + l.ToString() + "] = " + durs[l] + ";" + nl;
                    }
                    code += "	for (int i = 0; i < " + freqs.Length + "; i++)"
                        + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 0.30);"
                        + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}" + nl;
                }
                else
                {
                    code += "int nt[] = {" + frequencies.Remove(frequencies.Length - 1, 1) + "};" + nl + "int dr[] = {" + durations.Remove(durations.Length - 1, 1) + "};"
                                        + nl + "void setup()" + nl + "{" + nl + "	for (int i = 0; i < 10; i++)"
                                        + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 0.30);"
                                        + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}" + nl;
                }
                /*var measures = mxml.Parts[track_cbx.SelectedIndex].Measures;
                for(int m = 0; m < measures.Count; m++)
                {
                    var measure = measures[m];
                    foreach (var measure_element in measure.MeasureElements)
                    {
                        var element = measure_element.Element;
                        int dur = (int)GetPropertyVaue(element, "Duration");
                        bool ischord = (bool)GetPropertyVaue(element, "IsChordTone");
                        object pitch = GetPropertyVaue(element, "Pitch");
                        int freq = 0;
                        if (pitch != null)
                        {
                            string note = GetPropertyVaue(pitch, "Step").ToString();
                            bool alter = GetPropertyVaue(pitch, "Alter").ToString() == "1";
                            string octave = GetPropertyVaue(pitch, "Octave").ToString();

                            string nota = note;
                            if (alter)
                            {
                                nota = nota.ToLower();
                            }
                            nota += octave;
                            freq = (int)Math.Floor(NoteFrequency(nota));
                        }
                        if (!ischord)
                        {
                            switch (model_cbx.SelectedIndex)
                            {
                                case 0:
                                    durations[m] += dur + ",";
                                    frequencies[m] += freq + ",";
                                break;
                            }
                        }
                    }
                    if(m == 0)
                    {
                        /*code += frequencies[0].Remove(frequencies[0].Length - 1,1) + "};" + nl + durations[0].Remove(durations[0].Length -1, 1) + "};"
                            + nl + "void setup()" + nl + "{" + nl + "	for (int i = 0; i < " + measure.MeasureElements.Count.ToString() + "; i++)"
                            + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 1.30);"
                            + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}";*//*
                    }
                    else
                    {
                        /*code += nl + "	nt = { " + frequencies[m].Remove(frequencies[m].Length -1, 1) + " };" + nl + "	dr = { " + durations[m].Remove(durations[m].Length -1, 1) + " };"
                            + nl + "	for (int i = 0; i < " + measure.MeasureElements.Count.ToString() + "; i++)"
                            + nl + "	{" + nl + "		tone(" + output_cbx.Text + ", nt[i], dr[i]);" + nl + "		delay(dr[i] * 1.30);"
                            + nl + "		noTone(" + output_cbx.Text + ");" + nl + "	}";*//*
                    }
                }*/
                code += "}" + nl + "void loop(){}";
            }
            if(Directory.Exists(folder_txt.Text))
            {
                string projects_folder = folder_txt.Text + "\\";
                string project_name = Path.GetFileNameWithoutExtension(file_txt.Text).Replace(" ", "_");
                string project_folder = projects_folder + project_name;
                Directory.CreateDirectory(project_folder);
                File.WriteAllText(project_folder + "\\" + project_name + ".ino", code);
            }
        }
        private void Track_cbx_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            model_cbx.IsEnabled = true;
        }
        private void Model_cbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            output_cbx.IsEnabled = true;
            switch (model_cbx.SelectedIndex)
            {
                case 0: case 1:
                    for(int i = 2; i < 14; i++)
                    {
                        output_cbx.Items.Add(i.ToString());
                    }
                break;
            }
        }
        private void Output_cbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            browse_folder_btn.IsEnabled = true;
            folder_txt.IsEnabled = true;
        }
        private void Browse_folder_btn_Click(object sender, RoutedEventArgs e)
        {
            if (ifolder.ShowDialog() == true && Directory.Exists(ifolder.FileName))
            {
                folder_txt.Text = Path.GetFullPath(ifolder.FileName);
            }
            else
            {
                MessageBox.Show("Error seleccionar la carpeta.");
            }
            convert_btn.IsEnabled = true;
        }
    }
}
