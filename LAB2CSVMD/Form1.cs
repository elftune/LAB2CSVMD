using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DxLibDLL;

namespace LAB2CSVMD
{
    public partial class Form1 : Form
    {
        string APP_TITLE = "LAB2CSVMD ver. 0.2019.02.10.01";
        string sINIFile = "LAB2CSVMD.ini";
        string sLAB_Folder = "";
        static float fDEFAULT_IntervalTime = 0.5F;
        float fIntervalTieme = fDEFAULT_IntervalTime;
        Random rnd = new Random(Environment.TickCount);
        bool bOK = true;
        int nMovie = -1;
        int nMovieAllFrames = 0;
        string sMovieFile = "";
        List<ScData> listScData = new List<ScData>();
        int SIZE_X_MODE_1 = 525, SIZE_X_MODE_2 = 1124;

        public Form1()
        {
            InitializeComponent();

            this.Text = APP_TITLE;
            this.Width = SIZE_X_MODE_1;
            LoadFiles();
            textBox2.Text = fIntervalTieme.ToString("0.000");

            DX.SetUserWindow(pictureBox1.Handle);
            DX.ChangeWindowMode(DX.TRUE);
            bOK = (DX.DxLib_Init() == 0) ? true : false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (bOK == false) Close();
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);
        }

        private void saveINIFile()
        {
            string s = textBox2.Text;
            if (s == "") s = fDEFAULT_IntervalTime.ToString("0.000");
            try
            {
                fIntervalTieme = float.Parse(s);
            }
            catch
            {
                fIntervalTieme = fDEFAULT_IntervalTime;
            }
            finally
            {
                if (fIntervalTieme < 0.0F || fIntervalTieme > 100000.0F) fIntervalTieme = fDEFAULT_IntervalTime;
            }
            File.WriteAllText(@"./" + sINIFile, @"LAB_FOLDER=" + sLAB_Folder + Environment.NewLine + "INTERVAL_TIME=" + fIntervalTieme.ToString("0.000") + Environment.NewLine);
        }

        private void LoadFiles()
        {
            if (File.Exists(@"./" + sINIFile) == false)
            {
                saveINIFile();
                return;
            }

            string[] sLines = File.ReadAllLines(@"./" + sINIFile);
            // プチINIファイルの適当解析
            for (int i = 0; i < sLines.Length; i++)
            {
                string s = sLines[i], s0;
                s0 = "LAB_FOLDER=";
                if (s.StartsWith(s0) == true)
                {
                    sLAB_Folder = s.Substring(s0.Length).Trim();
                    if (sLAB_Folder.EndsWith(@"\") == false) sLAB_Folder += @"\";
                    if (sLAB_Folder == @"\") sLAB_Folder = "";
                    label1.Text = sLAB_Folder;
                }
                s0 = "INTERVAL_TIME=";
                if (s.StartsWith(s0) == true)
                {
                    try
                    {
                        fIntervalTieme = float.Parse(s.Substring(s0.Length).Trim());
                    }
                    catch
                    {
                        fIntervalTieme = fDEFAULT_IntervalTime;
                    }
                    finally
                    {
                        if (fIntervalTieme < 0.0F || fIntervalTieme > 100000.0F) fIntervalTieme = fDEFAULT_IntervalTime;
                    }
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            saveINIFile();
            if (nMovie != -1) DX.DeleteGraph(nMovie);
            DX.DxLib_End();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (sLAB_Folder != "") folderBrowserDialog1.SelectedPath = sLAB_Folder;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                sLAB_Folder = folderBrowserDialog1.SelectedPath;
                if (sLAB_Folder.EndsWith(@"\") == false) sLAB_Folder += @"\";
                label1.Text = sLAB_Folder;
            }
        }

        // LAB, TXT読み込み
        private void button2_Click(object sender, EventArgs e)
        {
            if (sLAB_Folder == "")
            {
                MessageBox.Show("フォルダが指定されていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // xxxxx.lab に対して xxxxx.wav, xxxxx.txt を検索
            string[] sLAB_FILES = Directory.GetFiles(sLAB_Folder, "*.lab");
            if (sLAB_FILES.Length == 0)
            {
                MessageBox.Show("音素情報の .lab ファイルが存在しません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            int nDummyCount = 0;
            for(int i=0; i<sLAB_FILES.Length; i++)
            {
                string sf = sLAB_FILES[i].Substring(0, sLAB_FILES[i].Length - 4); // xxxxxxxxxxxxxxxxxxx\xxxxxxx (--->.lab)
                if (File.Exists(sf + ".wav") == false)
                {
                    MessageBox.Show(sf + ".wav が見つかりません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (File.Exists(sf + ".txt") == false)
                {
                    if (DialogResult.Yes != MessageBox.Show(sf + ".txt が見つかりません。" + Environment.NewLine + Environment.NewLine +
                        sf + ".lab はソングデータですか？（ソングであれば、ダミーファイルを作成して続行します。）", APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        return;
                    }
                    else
                    {
                        string[] sDummys = { "ダミーNo." + nDummyCount++.ToString() };
                        File.WriteAllLines(sf + ".txt", sDummys, Encoding.Default);
                    }
                }
            }


            // 初期化
            textBox1.Text = "";
            listBox1.Items.Clear();
            listScData.Clear();
            if (nMovie != -1)
            {
                DX.DeleteGraph(nMovie);
                nMovie = -1;
                nMovieAllFrames = 0;
                sMovieFile = "";
            }

            string[] sLines = null;
            StringBuilder sb = new StringBuilder();
            for(int i=0; i<sLAB_FILES.Length; i++)
            {
                string s;
                string sTXTFile = sLAB_FILES[i].Substring(0, sLAB_FILES[i].Length - 4) + ".txt";
                sLines = File.ReadAllLines(sTXTFile, Encoding.Default);
                for(int j=0; j<sLines.Length; j++)
                {
                    sb.Append("# " + sLines[j] + Environment.NewLine);
                }

                sLines = File.ReadAllLines(sLAB_FILES[i], Encoding.Default);
                for (int j = 0; j < sLines.Length; j++)
                {
                    s = sLines[j]; // 0 850000 e
                    string[] sVars = s.Split(' ');
                    long l1, l2;
                    l1 = (long)double.Parse(sVars[0]); // 0
                    l2 = (long)double.Parse(sVars[1]); // 850000
                    l1 = l2 - l1; // 850000
                    float f = (float)((double)l1 / 10000000.0); // 0.085
                    sb.Append(sVars[2] + "," + f.ToString("0.000") + Environment.NewLine);
                }
                sb.Append("pau," + fIntervalTieme.ToString("0.000") + Environment.NewLine);
                sb.Append(Environment.NewLine); // 一行空ける

            }
            textBox1.Text = sb.ToString();
        }

        // CSV + VMD更新、連結WAV
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("LABデータが読み込まれていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string sFile = "";
            openFileDialog1.FileName = "";
            openFileDialog1.Title = "読み込むVMDファイルを選択してください。";
            openFileDialog1.Filter = "VMDファイル(*.vmd)|*.vmd";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                sFile = openFileDialog1.FileName;
            }
            else
            {
                return;
            }

            string[] sLines = null;

            // データ更新
            sLines = textBox1.Text.Split('\n');
            List<string> listLines = new List<string>();

            List<float>[] mouth_size = new List<float>[6]; // 口の大きさ(0.0～1.0)
            List<uint>[] face_val = new List<uint>[6]; // あ/い/う/え/お/(空白)/ん
            for (int i = 0; i < 6; i++)
            {
                mouth_size[i] = new List<float>();
                face_val[i] = new List<uint>();
            }

            ulong ulULTime = 0; // 実際の秒数(0.055とか)を10000000倍した数値(550,000)
            uint uiStartTimeFrame = 0, uiStartTimeFrameORG = 0;
            uint uiEndTimeFrame = 0, uiEndTimeFrameORG = 0;
            uint uiFrames = 0;
            uint uiSt = 0, uiEd = 0;
            List<ulong> listSentenceTime = new List<ulong>();
            List<ulong> listStartTime = new List<ulong>();
            ulong ulULFirstStartTime = 0;
            for (int j = 0; j < sLines.Length; j++)
            {
                string sss = sLines[j]; // 一行単位で切り出し
                sss = sss.Replace("\r", ""); // \r を除去しておく
                if (sss == "")
                {
                    listSentenceTime.Add(ulULTime); // 累計時間
                    continue;
                }
                if (sss.StartsWith("##StartTime,")== true)
                {
                    double d = double.Parse(sss.Substring(12));
                    ulULTime = (ulong)(d * 10000000.0 + 0.9); // 加算ではなく直接指定
                    if (ulULFirstStartTime == 0)
                    {
                        ulULFirstStartTime = ulULTime;
                    }
                    continue;
                }
                if (sss.StartsWith("# ") == true)
                {
                    listStartTime.Add(ulULTime);
                    continue;
                }
                if (sss.StartsWith("#") == true)
                {
                    MessageBox.Show(sss + " の行はスキップします。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                int nShapePrev = -1;

                string[] st1 = sss.Split(','); // "m" , "0.055"
                string sPhoneme = st1[0]; // "m"
                ulong ulULStartTime = ulULTime;
                ulong ulULEndTime = ulULStartTime + (ulong)(double.Parse(st1[1]) * 10000000.0 + 0.9);
                ulULTime = ulULEndTime;
//                System.Diagnostics.Trace.WriteLine("/音素: " + sPhoneme + " /開始: " + fStartTime.ToString("0.000") + " /終了: " + fEndTime.ToString("0.000"));
                uiStartTimeFrame = uiStartTimeFrameORG + (uint)((double)ulULStartTime / 10000000.0 * 30.0);
                uiEndTimeFrame = uiEndTimeFrameORG + (uint)((double)ulULEndTime / 10000000.0 * 30.0);

                // 口の形は何か
                string s = sPhoneme;
                int nShape = -1;
                if (s == "a" || s == "A") { nShape = 0; } // あ行
                if (s == "i" || s == "I") { nShape = 1; }
                if (s == "u" || s == "U") { nShape = 2; }
                if (s == "e" || s == "E") { nShape = 3; }
                if (s == "o" || s == "O") { nShape = 4; }
                if (s == "N") { nShape = 5; } // ん
                if (s == "pau") { nShape = -100; } // pause

                // 子音とかかも
                if ((nShape == -1) && (j < sLines.Length - 1))
                {
                    string sss2 = sLines[j + 1].Replace("\r", "");
                    st1 = sss2.Split(','); // "m" , "0.055"
                    sPhoneme = st1[0]; // "m"
                    ulULEndTime = ulULEndTime + (ulong)(double.Parse(st1[1]) * 10000000.0 + 0.9); // fStartはいじる必要なし
                    ulULTime = ulULEndTime;
                    s = sPhoneme;
                    if (s == "a" || s == "A") { nShape = 0; }
                    if (s == "i" || s == "I") { nShape = 1; }
                    if (s == "u" || s == "U") { nShape = 2; }
                    if (s == "e" || s == "E") { nShape = 3; }
                    if (s == "o" || s == "O") { nShape = 4; }
                    if (nShape >= 0)
                    {
                        uiEndTimeFrame = uiEndTimeFrameORG + (uint)((double)ulULEndTime / 10000000.0 * 30.0);
//                        System.Diagnostics.Trace.WriteLine("/音素: " + sPhoneme + " /開始: " + fStartTime.ToString("0.000") + " /終了: " + fEndTime.ToString("0.000"));
                        j = j + 1; // 子音の場合は次の母音までを1セットとする
                    }
                    else
                    if (j < sLines.Length - 2)
                    {
                        // ソリッド ⇒ s/o/r/i/cl/d/o なので+2が必要
                        sss2 = sLines[j + 2].Replace("\r", ""); ;
                        st1 = sss2.Split(','); // "m" , "0.055"
                        sPhoneme = st1[0]; // "m"
                        ulULEndTime = ulULEndTime + (ulong)(double.Parse(st1[1]) * 10000000.0 + 0.9); // fStartはいじる必要なし
                        ulULTime = ulULEndTime;
                        s = sPhoneme;
                        if (s == "a" || s == "A") { nShape = 0; }
                        if (s == "i" || s == "I") { nShape = 1; }
                        if (s == "u" || s == "U") { nShape = 2; }
                        if (s == "e" || s == "E") { nShape = 3; }
                        if (s == "o" || s == "O") { nShape = 4; }
                        if (nShape >= 0)
                        {
                            uiEndTimeFrame = uiEndTimeFrameORG + (uint)((double)ulULEndTime / 10000000.0 * 30.0);
//                            System.Diagnostics.Trace.WriteLine("/音素: " + sPhoneme + " /開始: " + fStartTime.ToString("0.000") + " /終了: " + fEndTime.ToString("0.000"));
                            j = j + 2; // 子音の場合は次の母音までを1セットとする
                        }
                    }
                }

                while (uiFrames < uiStartTimeFrame)
                {
                    uiFrames++;
                }
                // ここの uiFrames が開始フレーム
                uiSt = uiFrames;

                int nCount = 0;
                float fValue = 0.0F;
                if (nShape >= 0)
                {
                    while (uiFrames < uiEndTimeFrame)
                    {
                        switch (nCount)
                        {
                            case 0:
                                fValue = 0.66F;
                                if (uiFrames > 0)
                                {
                                    face_val[nShape].Add(uiFrames - 1);
                                    if (nShapePrev == nShape)
                                        mouth_size[nShape].Add(0.33F);
                                    else
                                        mouth_size[nShape].Add(0.0F);
                                }
                                face_val[nShape].Add(uiFrames);
                                mouth_size[nShape].Add(fValue);
                                break;
                            case 1:
                                fValue = 1.0F;
                                face_val[nShape].Add(uiFrames);
                                mouth_size[nShape].Add(fValue);
                                break;
                            default:
                                fValue = 1.0F;
                                break;
                        }
                        uiFrames++; nCount++;
                    }

                    // 終了フレームは次の音素の開始。なので前の音素は 0.00 にしておく
                    face_val[nShape].Add(uiFrames);
                    mouth_size[nShape].Add(0.0F);

                }
                else
                {
                    while (uiFrames < uiEndTimeFrame)
                    {
                        uiFrames++;
                    }
                }

                // ここの uiFrames が終了フレーム
                uiEd = uiFrames;
                nShapePrev = nShape;
                ulULTime = ulULEndTime;
            }

            // 書き出し
            List<string> ss = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                string s = "";
                switch (i)
                {
                    case 0:
                        s = "あ";
                        break;
                    case 1:
                        s = "い";
                        break;
                    case 2:
                        s = "う";
                        break;
                    case 3:
                        s = "え";
                        break;
                    case 4:
                        s = "お";
                        break;
                    case 5:
                        s = "ん";
                        break;
                }
                for (int j = 0; j < face_val[i].Count; j++)
                {
                    ss.Add(s + "," + face_val[i][j] + "," + mouth_size[i][j]);
                }
            }

            // まばたきも入れよう
            // uiFrames 約10秒単位でまばたき (300)ごと 1秒30コマ
            uint uiFrame2 = 0;
            while (uiFrame2 < uiFrames)
            {
                int r = 30 * 8 + rnd.Next(120 + 1); // 8～12秒間隔
                if (uiFrame2 + (uint)r < uiFrames)
                {
                    ss.Add("まばたき," + (uiFrame2 + (uint)r + 0) + ",0");
                    ss.Add("まばたき," + (uiFrame2 + (uint)r + 25) + ",0.11");
                    ss.Add("まばたき," + (uiFrame2 + (uint)r + 30) + ",0.35");
                    ss.Add("まばたき," + (uiFrame2 + (uint)r + 35) + ",0.11");
                    ss.Add("まばたき," + (uiFrame2 + (uint)r + 60) + ",0");
                }
                uiFrame2 += (uint)r;
            }

            sLines = ss.ToArray();


            // VMD読み込み＋書き出し
            //
            string sFile2 = sFile.Substring(0, sFile.Length - 4) + "_改.vmd";
            VMD vmd = new VMD();
            if (vmd.Load(sFile) == false)
            {
                MessageBox.Show(sFile + " を読み込めません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // モーフの中に "_DUMMY_1","_DUMMY_2" があるか
            int n1 = -1, n2 = -1;
            for (int i = 0; i < (int)vmd.uiSkinCount; i++)
            {
                string s = vmd.clsSkin[i].sSkinName;
                if (s == "_DUMMY_1") n1 = i;
                if (s == "_DUMMY_2") n2 = i;
            }


            VMD_SKIN[] newSkin = null;
            if (n1 >=0 && n2 >= 0)
            {
                newSkin = new VMD_SKIN[vmd.uiSkinCount - (n2 - n1 + 1) + sLines.Length + 2];
            }
            else
            {
                newSkin = new VMD_SKIN[vmd.uiSkinCount + sLines.Length + 2];
            }
            int nPtr = 0;
            for (int i = 0; i < n1; i++)
            {
                newSkin[nPtr] = new VMD_SKIN();
                newSkin[nPtr].SkinName = vmd.clsSkin[i].SkinName;
                newSkin[nPtr].FrameNo = vmd.clsSkin[i].FrameNo;
                newSkin[nPtr].Weight = vmd.clsSkin[i].Weight;
                nPtr++;
            }
            for (int i = n2 + 1; i < (int)vmd.uiSkinCount; i++)
            {
                newSkin[nPtr] = new VMD_SKIN();
                newSkin[nPtr].SkinName = vmd.clsSkin[i].SkinName;
                newSkin[nPtr].FrameNo = vmd.clsSkin[i].FrameNo;
                newSkin[nPtr].Weight = vmd.clsSkin[i].Weight;
                nPtr++;
            }

            // 追加分
            byte[] bt = null;
            newSkin[nPtr] = new VMD_SKIN();
            bt = Encoding.GetEncoding("Shift_JIS").GetBytes("_DUMMY_1");
            for (int i = 0; i < bt.Length; i++) newSkin[nPtr].SkinName[i] = bt[i];
            newSkin[nPtr].FrameNo = 0;
            newSkin[nPtr].Weight = 0;
            nPtr++;

            for (int i = 0; i < sLines.Length; i++)
            {
                newSkin[nPtr] = new VMD_SKIN();
                string s = sLines[i];
                string[] sv = s.Split(','); // "まばたき" "0" "1.0"
                bt = Encoding.GetEncoding("Shift_JIS").GetBytes(sv[0]);
                for (int j = 0; j < bt.Length; j++) newSkin[nPtr].SkinName[j] = bt[j];
                newSkin[nPtr].FrameNo = UInt32.Parse(sv[1]);
                newSkin[nPtr].Weight = Single.Parse(sv[2]);
                nPtr++;
            }

            newSkin[nPtr] = new VMD_SKIN();
            bt = Encoding.GetEncoding("Shift_JIS").GetBytes("_DUMMY_2");
            for (int i = 0; i < bt.Length; i++) newSkin[nPtr].SkinName[i] = bt[i];
            newSkin[nPtr].FrameNo = 0;
            newSkin[nPtr].Weight = 0;

            vmd.uiSkinCount = (uint)newSkin.Length;
            vmd.clsSkin = newSkin;
            if (vmd.Save(sFile2, checkBox3.Checked) == false)
            {
                MessageBox.Show(sFile2 + " の書き出しに失敗しました。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                MessageBox.Show(sFile2 + " を書き出しました。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }


            // CSV更新
            //
            string sCSVFile = sFile.Substring(0, sFile.Length - 4) + "_改.csv";
            string[] sLines3 = vmd.list.ToArray();
            File.WriteAllLines(sCSVFile, sLines3, Encoding.Default);


            // 連結.wav作成
            if (checkBox1.Checked == false) return;

            List<int> anSSound = new List<int>();
            List<int> anSSoundSize = new List<int>();
            string[] sLAB_FILES = Directory.GetFiles(sLAB_Folder, "*.lab");
            int nSize = 0, nID = -1;
            for (int i=0; i<sLAB_FILES.Length; i++)
            {
                string sWAVFile = sLAB_FILES[i].Substring(0, sLAB_FILES[i].Length - 4) + ".wav";
//                if (sWAVFile.IndexOf("結合wav_") >= 0) continue;
                nID = DX.LoadSoftSound(sWAVFile);
                if (nID == -1)
                {
                    MessageBox.Show(sWAVFile + " の読み込みでエラーが発生しました。中止します。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    DX.InitSoftSound();
                    return;
                }
                int ch, bps, rt, f;
                DX.GetSoftSoundFormat(nID, out ch, out bps, out rt, out f);
                if (!(ch == 1 && bps == 16 && rt == 48000))
                {
                    MessageBox.Show(sWAVFile + " はデータフォーマットが異なります。中止します。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    DX.InitSoftSound();
                    return;
                }

                int n = DX.GetSoftSoundSampleNum(nID); // 1ch, 16bit, 48000Hz ... 1秒間に 48000*2*1byte
                float ff1 = (float)n / 48000.0F; // WAVEファイルから取得秒数
                float ff2 = 0.0F;
                if (i == 0)
                {
                    ff2 = (float)((double)(listSentenceTime[0] / 10000000.0)); // 3.6999984 --> 3699.984 +0.999
                }
                else
                {
                    ff2 = (float)((double)((listSentenceTime[i] - listSentenceTime[i - 1]) / 10000000.0)); // 3.6999984 --> 3699.984 +0.999
                }

                n = (int)(ff2 * 48000.0F);
                anSSoundSize.Add(n);
                nSize += n;
                anSSound.Add(nID);
            }
            nSize = (int)((double)(ulULTime * 48000) / 10000000.0);
            if (nSize <= 0)
            {
                MessageBox.Show("書き込むデータサイズが小さすぎます。終了します。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (nSize > 3600 * 48000 * 10) // 10時間
            {
                MessageBox.Show("書き込むデータサイズが大きすぎます。終了します。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int nSSID = DX.MakeSoftSoundCustom(1, 16, 48000, nSize); // CeVIOの仕様
            for (int i = 0; i < nSize; i++) // まず0で全埋め
            {
                DX.WriteSoftSoundData(nSSID, i, 0, 0);
            }

            nPtr = 0;
            int h = 0;
            for (int i = 0; i < sLAB_FILES.Length; i++)
            {
//                if (sWAV_FILES[i].IndexOf("結合wav_") >= 0) continue;
                nPtr = (int)((double)listStartTime[h] / 10000000.0 * 48000.0);
                nID = anSSound[h]; // 読み出すSoftSoundのID

                int n = DX.GetSoftSoundSampleNum(nID); // 1ch, 16bit, 48000Hz ... 1秒間に 48000*2*1byte
                for (int j = 0; j < n; j++)
                {
                    int ch1, ch2, v = DX.ReadSoftSoundData(nID, j, out ch1, out ch2);
                    int ch1Z, ch2Z;
                    DX.ReadSoftSoundData(nSSID, nPtr, out ch1Z, out ch2Z);
                    ch1 += ch1Z; ch2 += ch2Z;
                    DX.WriteSoftSoundData(nSSID, nPtr, ch1, ch2);
                    nPtr++;
                    if (nPtr > nSize)
                    {
                        MessageBox.Show("書き込むデータサイズがバッファサイズより大きいため、途中までの内容でデータを書きだします。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                }
                h++;
            }

            sFile = sLAB_Folder + "結合wav_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
            if (DX.SaveSoftSound(nSSID, sFile) == 0)
                MessageBox.Show(sFile + " を作成しました。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(sFile + " の作成中にエラーが発生しました。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            DX.InitSoftSound();
        }


        // 再生
        private void button4_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            int n = DX.GetMovieStateToGraph(nMovie);
            if (n == 0) // 停止中
            {
                int nNow = DX.TellMovieToGraphToFrame(nMovie);
                int nTotal = DX.GetMovieTotalFrameToGraph(nMovie);
                if (nNow == nTotal - 1)
                {
                    DX.SeekMovieToGraphToFrame(nMovie, 0);
                }
                DX.PlayMovieToGraph(nMovie, DX.DX_PLAYTYPE_BACK);
                button4.Text = "停止";
                timer1.Interval = 16; // 62fps相当
                timer1.Start();
            }
            if (n == 1) // 再生中
            {
                DX.PauseMovieToGraph(nMovie);
                button4.Text = "再生";
                timer1.Stop();
            }
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            if (nMovie == -1) return;
            DX.SeekMovieToGraphToFrame(nMovie, e.NewValue);
            UpdateMovie();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (nMovie == -1) return;
            if (checkBox2.Checked == true)
                DX.ChangeMovieVolumeToGraph(0, nMovie);
            else
                DX.ChangeMovieVolumeToGraph(255, nMovie);
        }

        private void UpdateMovie()
        {
            if (nMovie == -1) return;

            int n1 = DX.TellMovieToGraphToFrame(nMovie);
            int n2 = DX.TellMovieToGraph(nMovie); // msec

            DX.ClearDrawScreen();
            DX.DrawExtendGraph(0, 0, 480, 272, nMovie, DX.FALSE);
            int y = 128;
            for(int i=0; i< listScData.Count; i++)
            {
                ulong ul = listScData[i].ulStartTime; // 1000000系
                int nn = (int)(ul / 10000);
                // 3700msec に対し 3703mとか     
                if (n2 >= nn)
                {
                    if (n2 - nn < 33)
                    {
                        DX.DrawString(16, y, listScData[i].sScript, DX.GetColor(255, 0, 0));
                        y += 16;
                    }
                    else
                    if (n2 - nn < 500)
                    {
                        DX.DrawString(16, y, listScData[i].sScript, DX.GetColor(0, 255, 255));
                        y += 16;
                    }
                }
            }
            DX.ScreenFlip();

            hScrollBar1.Value = n1;
            label4.Text = n1 + "ﾌﾚｰﾑ";
            label3.Text = ((double)n2 / 1000.0).ToString("0.000") + "秒";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateMovie();
            int n1 = DX.TellMovieToGraphToFrame(nMovie);
            if (n1 + 1 >= nMovieAllFrames)
            {
                DX.PauseMovieToGraph(nMovie);
                button4.Text = "再生";
                timer1.Stop();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            int n = DX.TellMovieToGraphToFrame(nMovie);
            if (n > 0)
            {
                DX.SeekMovieToGraphToFrame(nMovie, n - 1);
            }
            UpdateMovie();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            DX.SeekMovieToGraphToFrame(nMovie, 0);
            UpdateMovie();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            int n = DX.TellMovieToGraphToFrame(nMovie);
            if (n < nMovieAllFrames - 1)
            {
                DX.SeekMovieToGraphToFrame(nMovie, n + 1);
            }
            UpdateMovie();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DX.SeekMovieToGraphToFrame(nMovie, nMovieAllFrames - 1);
            UpdateMovie();
        }

        // 動画読み込み
        private void button11_Click(object sender, EventArgs e)
        {
            if (nMovie != -1)
            {
                DX.DeleteGraph(nMovie);
                nMovie = -1;
            }

            MessageBox.Show("読み込み動画ファイルは、H.264/AVCのMP4、OGV/OGX のみ対応しています（H.265/HEVCのMP4は対応していません）。他の形式でも読み込めますが、仕様上、シークできないため意味がありません。" + Environment.NewLine +
                "また、シーク処理が重いため 480x272 程度の解像度の低い動画の仕様を強く推奨します。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
            openFileDialog1.Filter = "H.264(mp4), OGV(ogv/x)|*.mp4;*.ogv;*.ogx|全てのファイル|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.Title = "読み込み動画ファイルを選択してください。";
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                sMovieFile = openFileDialog1.FileName;
            }
            else
            {
                return;
            }

            nMovie = DX.LoadGraph(sMovieFile);
            if (nMovie == -1)
            {
                MessageBox.Show(sMovieFile + " が開けません。(H.264/AVC, OGV/OGX のみ対応)", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            nMovieAllFrames = DX.GetMovieTotalFrameToGraph(nMovie);
            if (nMovieAllFrames == -1)
            {
                DX.DeleteGraph(nMovie);
                nMovie = -1;
                MessageBox.Show(sMovieFile + " はシークできないため、使用できません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            hScrollBar1.Minimum = 0;
            hScrollBar1.Maximum = nMovieAllFrames;

            if (checkBox2.Checked == true)
                DX.ChangeMovieVolumeToGraph(nMovie, 0);
            else
                DX.ChangeMovieVolumeToGraph(nMovie, 255);

            hScrollBar1.Value = 0;
            label4.Text = "0ﾌﾚｰﾑ";
            label3.Text = "0秒";

            UpdateMovie();
        }

        // ムービー側に [更新]
        private void button9_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("LABファイルが読み込まれていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            listScData.Clear();
            string[] sLines = textBox1.Text.Split('\r');
            ulong ulTime = 0;
            ScData scData = null;
            for (int i=0; i<sLines.Length; i++)
            {
                string s = sLines[i];
                s = s.Replace("\n", ""); // 改行を消しておく
                double d = 0.0;
                if (s.StartsWith("##StartTime,") == true)
                {
                    d = double.Parse(s.Substring(12)); // 1.500
                    ulTime = (ulong)(d * 10000000.0 + 0.9); // 加算ではなく直接指定
                    scData = new ScData();
                    scData.ulStartTime = ulTime; // 1.500 --> 15000000
                    scData.sScript = "";
                    scData.nStartFrame = -1;
                    continue;
                }
                if (s.StartsWith("# ") == true)
                {
                    if (scData == null)
                    {
                        scData = new ScData();
                        scData.ulStartTime = ulTime;
                        scData.nStartFrame = -1;
                    }
                    scData.sScript = s.Substring(2);
                    continue;
                }
                if (s == "")
                {
                    if (scData != null)
                    {
                        listScData.Add(scData);
                        scData = null;
                    }
                    continue;
                }

                ulong ul = (ulong)(double.Parse(s.Split(',')[1]) * 10000000.0 + 0.9);
                ulTime += ul;
            }

            listBox1.Items.Clear();
            for(int i=0; i<listScData.Count; i++)
            {
                int n1 = (int)(listScData[i].ulStartTime / 10000); // 3700msec
                DX.SeekMovieToGraph(nMovie, n1); // まず動かす
                int n2 = DX.TellMovieToGraph(nMovie); // 3674
                if (n2 < n1) // 未満なら、1コマ進める
                {
                    DX.SeekMovieToGraphToFrame(nMovie, DX.TellMovieToGraphToFrame(nMovie) + 1);
                }

//                UpdateMovie();
                listScData[i].nStartFrame = DX.TellMovieToGraphToFrame(nMovie);
                listScData[i].ulStartTimeOnMovie = (ulong)DX.TellMovieToGraph(nMovie) * 10000;
                string s = i.ToString("000") + ":" + ((double)listScData[i].ulStartTime / 10000000.0).ToString("0.000") + "(" +
                    ((double)listScData[i].ulStartTimeOnMovie / 10000000.0).ToString("0.000") + "):" +
                    listScData[i].nStartFrame + ":" + listScData[i].sScript;
                listBox1.Items.Add(s);
            }
            DX.SeekMovieToGraphToFrame(nMovie, 0);
            UpdateMovie();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("LABファイルが読み込まれていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("リストボックスからセリフを1つ選択してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string s = listBox1.SelectedItem.ToString();
            int n = Int32.Parse(s.Substring(0, 3)); // "002"
            DX.SeekMovieToGraphToFrame(nMovie, listScData[n].nStartFrame);
            UpdateMovie();
        }

        // TextBoxに更新
        private void button10_Click(object sender, EventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("LABファイルが読み込まれていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox1.Items.Count == 0)
            {
                MessageBox.Show("リストボックスからセリフを1つ選択してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string[] sLines = textBox1.Text.Split('\r');
            List<string> list = new List<string>();
            int j = 0;
            double d = -100.0;
            for (int i = 0; i < sLines.Length; i++)
            {
                string s = sLines[i];
                s = s.Replace("\n", ""); // 改行を消しておく
                if (s.StartsWith("##StartTime,") == true)
                {
                    d = (double)listScData[j].ulStartTime / 10000000.0; // 1.500
                    list.Add("##StartTime," + d.ToString("0.000"));
                    continue;
                }
                if (s.StartsWith("# ") == true)
                {
                    if (d < 0)
                    {
                        // ##StartTimeはここで設定する
                        d = (double)listScData[j].ulStartTime / 10000000.0; // 1.500
                        list.Add("##StartTime," + d.ToString("0.000"));

                    }
                    list.Add(s);
                    continue;
                }
                if (s == "")
                {
                    if (d >= 0.0)
                    {
                        d = -100.0;
                        j++;
                    }
                    list.Add(s);
                    continue;
                }
                list.Add(s);
            }
            string[] ss = list.ToArray();
            StringBuilder sb = new StringBuilder();
            for(int i=0; i<ss.Length; i++)
            {
                sb.Append(ss[i] + Environment.NewLine);
            }
            textBox1.Text = sb.ToString();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (this.Width == SIZE_X_MODE_1)
            {
                this.Width = SIZE_X_MODE_2;
                button12.Text = "<<";
            }
            else
            {
                this.Width = SIZE_X_MODE_1;
                button12.Text = ">>";
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        // 確定
        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (nMovie == -1)
            {
                MessageBox.Show("動画ファイルが指定されていません。H.264/AVCのMP4, OGV/OGX で、480x272程度の低解像度のものを指定してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("LABファイルが読み込まれていません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox1.Items.Count == 0)
            {
                MessageBox.Show("[更新→] を実行してください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("リストボックスからセリフを1つ選択してから、動画をダブルクリックしてください。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int i = listBox1.SelectedIndex;
            int n = DX.TellMovieToGraph(nMovie); // 表示上の時間（3.703） 3703msec
            ulong ulPrevTime = listScData[i].ulStartTimeOnMovie;
            int n1 = DX.TellMovieToGraphToFrame(nMovie);
            listScData[i].ulStartTimeOnMovie = (ulong)n * 10000;
            ulong ulNewTime = listScData[i].ulStartTimeOnMovie;
            n -= 15; // 前のフレームと今のフレームの中間
            if (n < 0) n = 0;
            listScData[i].ulStartTime = (ulong)n * 10000;
            int nDelta = n1 - listScData[i].nStartFrame;
            listScData[i].nStartFrame = n1;

            string s = i.ToString("000") + ":" + ((double)listScData[i].ulStartTime / 10000000.0).ToString("0.000") + "(" +
                ((double)listScData[i].ulStartTimeOnMovie / 10000000.0).ToString("0.000") + "):" +
                listScData[i].nStartFrame + ":" + listScData[i].sScript;
            listBox1.Items[i] = s;

            // あとのセリフの位置をずらすか
            if (ulNewTime >= ulPrevTime)
            {
                ulong ulDelta = ulNewTime - ulPrevTime; // 10^7
                double d = (double)ulDelta / 10000000.0; // x.xxx秒
                s = "セリフ " + i.ToString() + " の開始時間を " + d.ToString("0.000") + " 秒 後ろに下げました。" + Environment.NewLine +
                    (i + 1).ToString() + " 以降のセリフにも全て同じだけ下げますか？";
                if (MessageBox.Show(s, APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    for (int k = i + 1; k < listScData.Count; k++)
                    {
                        listScData[k].ulStartTimeOnMovie += ulDelta;
                        listScData[k].ulStartTime += ulDelta;
                        listScData[k].nStartFrame += nDelta;

                        s = k.ToString("000") + ":" + ((double)listScData[k].ulStartTime / 10000000.0).ToString("0.000") + "(" +
                            ((double)listScData[k].ulStartTimeOnMovie / 10000000.0).ToString("0.000") + "):" +
                            listScData[k].nStartFrame + ":" + listScData[k].sScript;
                        listBox1.Items[k] = s;

                    }
                }
            }
            else {
                ulong ulDelta = ulPrevTime - ulNewTime; // 10^7
                double d = (double)ulDelta / 10000000.0; // x.xxx秒
                s = "セリフ " + i.ToString() + " の開始時間を " + d.ToString("0.000") + " 秒 前に上げました。" + Environment.NewLine +
                    (i + 1).ToString() + " 以降のセリフにも全て同じだけ前に上げますか？";
                if (MessageBox.Show(s, APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    for (int k = i + 1; k < listScData.Count; k++)
                    {
                        listScData[k].ulStartTimeOnMovie -= ulDelta;
                        listScData[k].ulStartTime -= ulDelta;
                        listScData[k].nStartFrame += nDelta;

                        s = k.ToString("000") + ":" + ((double)listScData[k].ulStartTime / 10000000.0).ToString("0.000") + "(" +
                            ((double)listScData[k].ulStartTimeOnMovie / 10000000.0).ToString("0.000") + "):" +
                            listScData[k].nStartFrame + ":" + listScData[k].sScript;
                        listBox1.Items[k] = s;

                    }
                }
            }
        }
    }

    public class ScData
    {
        public string sScript; // セリフ
        public int nStartFrame; // 発音開始フレーム
        public ulong ulStartTime; // 発音開始 10^7系統
        public ulong ulStartTimeOnMovie; // 発音開始 10^7系統 (動画時間上)
    }
}
