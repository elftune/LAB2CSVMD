using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;


// 参照追加　resentationCore.DLL

// VMD構造解析資料　... 情報ありがとうございます。
// https://github.com/hangingman/wxMMDViewer/tree/master/libvmdconv
// https://blog.goo.ne.jp/torisu_tetosuki/e/bc9f1c4d597341b394bd02b64597499d
// https://harigane.at.webry.info/201103/article_1.html

public class VMD
{
    public VMD_HEADER clsHeader;
    public uint uiMotionCount;
    public VMD_MOTION[] clsMotion;
    public uint uiSkinCount;
    public VMD_SKIN[] clsSkin;
    public uint uiCameraCount;
    public VMD_CAMERA[] clsCamera;
    public uint uiLightCount;
    public VMD_LIGHT[] clsLight;
    public uint uiSelfShadowCount;
    public VMD_SELF_SHADOW[] clsSelfShadow;
    public uint uiShowIKCount;
    public VMD_SHOWIKFRAME[] clsShowIK;

    public List<string> list = new List<string>();
    static float M_PI = 3.14159265358979323846F;

    public VMD()
    {
        clsHeader = new VMD_HEADER();
        uiMotionCount = 0;
        clsMotion = null;
        uiSkinCount = 0;
        clsSkin = null;
        uiCameraCount = 0;
        clsCamera = null;
        uiLightCount = 0;
        clsLight = null;
        uiSelfShadowCount = 0;
        clsSelfShadow = null;
        uiShowIKCount = 0;
        clsShowIK = null;
    }

    int GetLength(byte[] bPtr)
    {
        int n = bPtr.Length;
        for(int i=n-1; i>0; i--)
        {
            if (bPtr[i] != 0)
            {
                n = i + 1;
                break;
            }
        }
        return n;
    }

    float ToDegree(float x)
    {
        return x / M_PI * 180.0f;
    }

    // bCompatibleData = true ---> VMD Converter Graphical(っぽい)データで出力
    public bool Save(string sFile, bool bCompatibleData = true)
    {
        int nSize = 0;
        nSize += 30 + 20; // HEADER

        nSize += 4; // MOTION_COUNT
        nSize += (int)uiMotionCount * (15 + 4 + 4 * 3 + 4 * 4 + 64); // MOTION

        nSize += 4; // SKIN_COUNT
        nSize += (int)uiSkinCount * (15 + 4 + 4); // MOTION

        nSize += 4; // CAMERA_COUNT
        nSize += (int)uiCameraCount * (4 + 4 + 4 * 3 + 4 * 3 + 24 + 4 + 1); // Camera

        nSize += 4; // LIGHT_COUNT
        nSize += (int)uiLightCount * (4 + 4 * 3 + 4 * 3); // Light

        nSize += 4; // SELFSHADOW_COUNT
        nSize += (int)uiSelfShadowCount * (4 + 1 + 4);

        nSize += 4; // IK
        for(int i=0; i< (int)uiShowIKCount; i++)
        {
            nSize += 4 + 1 + 4;
            for(int j=0; j<clsShowIK[i].ik_count; j++)
            {
                nSize += 20 + 1;
            }
        }

        byte[] buffer = null;
        list.Clear();
        StringBuilder sb = null;
        try
        {
            using (var fs = new FileStream(sFile, FileMode.Create, FileAccess.Write))
            {
                buffer = new byte[nSize];
                int nPtr = 0, i = 0, j = 0;

                // HEADER
                nSize = 30;
                for (i = 0; i < nSize; i++)
                {
                    buffer[nPtr + i] = clsHeader.VmdHeader[i];
                }
                list.Add(Encoding.GetEncoding("shift_jis").GetString(clsHeader.VmdHeader, 0, GetLength(clsHeader.VmdHeader)));
                nPtr += nSize;

                nSize = 20;
                for (i = 0; i < nSize; i++)
                {
                    buffer[nPtr + i] = clsHeader.VmdModelName[i];
                }
                list.Add(Encoding.GetEncoding("shift_jis").GetString(clsHeader.VmdModelName, 0, GetLength(clsHeader.VmdModelName)));
                nPtr += nSize;

                byte[] pByte = null;

                // MOTION
                nSize = 4;
                pByte = BitConverter.GetBytes(uiMotionCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiMotionCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiMotionCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 15;
                    for (i = 0; i < nSize; i++)
                    {
                        buffer[nPtr + i] = clsMotion[j].BoneName[i];
                    }
                    sb.Append(Encoding.GetEncoding("shift_jis").GetString(clsMotion[j].BoneName, 0, GetLength(clsMotion[j].BoneName)) + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsMotion[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsMotion[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    for (int k = 0; k < 3; k++)
                    {
                        nSize = 4;
                        pByte = BitConverter.GetBytes(clsMotion[j].Location[k]);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        sb.Append(clsMotion[j].Location[k].ToString() + ",");
                        nPtr += nSize;
                    }
                    // VMDバイナリはQuarternion(4つ)だが、VMDConverter(Graphocal)は角度(3つ)
                    if (bCompatibleData == true)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            nSize = 4;
                            pByte = BitConverter.GetBytes(clsMotion[j].Rotatation[k]);
                            for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                            nPtr += nSize;
                        }

                        // Quarternion clsMotion[j].Rotatation[0～3] から角度に変換
                        Quaternion q = new Quaternion((double)clsMotion[j].Rotatation[0], (double)clsMotion[j].Rotatation[1], (double)clsMotion[j].Rotatation[2], (double)clsMotion[j].Rotatation[3]);
                        Matrix3D m = new Matrix3D();
                        m.Rotate(q);
                        double roll = Math.Atan2(m.M12, m.M22);
                        double pitch = Math.Asin(-m.M32);
                        double yaw = Math.Atan2(m.M31, m.M33);
                        if (Math.Abs(Math.Cos(pitch)) < 1.0e-6f) {
                            roll += m.M12 > 0.0 ? M_PI : -M_PI;
                            yaw += m.M31 > 0.0 ? M_PI: -M_PI;
                        }

                        float[] fAngles = new float[3];
                        fAngles[0] = (float)pitch;
                        fAngles[1] = (float)yaw;
                        fAngles[2] = (float)roll;
                        for (int k=0; k<3; k++)
                        {
                            sb.Append(ToDegree(fAngles[k]).ToString() + ",");
                        }
                    }
                    else
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            nSize = 4;
                            pByte = BitConverter.GetBytes(clsMotion[j].Rotatation[k]);
                            for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                            sb.Append(clsMotion[j].Rotatation[k].ToString() + ",");
                            nPtr += nSize;
                        }
                    }

                    nSize = 64;
                    sb.Append("0x");
                    for (i = 0; i < nSize; i++)
                    {
                        buffer[nPtr + i] = clsMotion[j].Interpolation[i];
                        sb.Append(buffer[nPtr + i].ToString("X2"));
                    }
                    nPtr += nSize;

                    list.Add(sb.ToString());
                }

                // SKIN
                nSize = 4;
                pByte = BitConverter.GetBytes(uiSkinCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiSkinCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiSkinCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 15;
                    for (i = 0; i < nSize; i++)
                    {
                        buffer[nPtr + i] = clsSkin[j].SkinName[i];
                    }
                    sb.Append(Encoding.GetEncoding("shift_jis").GetString(clsSkin[j].SkinName, 0, GetLength(clsSkin[j].SkinName)) + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsSkin[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsSkin[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsSkin[j].Weight);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsSkin[j].Weight.ToString() + ((i < nSize - 1) ? "," : ""));
                    nPtr += nSize;

                    list.Add(sb.ToString());
                }

                // Camera
                nSize = 4;
                pByte = BitConverter.GetBytes(uiCameraCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiCameraCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiCameraCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsCamera[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsCamera[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsCamera[j].Length);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsCamera[j].Length.ToString() + ",");
                    nPtr += nSize;

                    for (int k = 0; k < 3; k++)
                    {
                        nSize = 4;
                        pByte = BitConverter.GetBytes(clsCamera[j].Location[k]);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        sb.Append(clsCamera[j].Location[k].ToString() + ",");
                        nPtr += nSize;
                    }
                    for (int k = 0; k < 3; k++)
                    {
                        nSize = 4;
                        pByte = BitConverter.GetBytes(clsCamera[j].Rotation[k]);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        if (bCompatibleData == true)
                        {
                            sb.Append(ToDegree(clsCamera[j].Rotation[k]).ToString() + ",");
                        }
                        else
                        {
                            sb.Append(clsCamera[j].Rotation[k].ToString() + ",");
                        }
                        nPtr += nSize;
                    }

                    nSize = 24;
                    sb.Append("0x");
                    for (i = 0; i < nSize; i++)
                    {
                        buffer[nPtr + i] = clsCamera[j].Interpolation[i];
                        sb.Append(buffer[nPtr + i].ToString("X2"));
                    }
                    nPtr += nSize;
                    if (bCompatibleData == false)
                        sb.Append(",");

                    nSize = 4;
                    if (bCompatibleData == false)
                    {
                        pByte = BitConverter.GetBytes(clsCamera[j].ViewingAngle);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        sb.Append(clsCamera[j].ViewingAngle.ToString() + ",");
                    }
                    else
                    {
                        pByte = BitConverter.GetBytes(clsCamera[j].ViewingAngle);
                        for (i = 0; i < nSize; i++)
                        {
                            buffer[nPtr + i] = pByte[i];
                            sb.Append(buffer[nPtr + i].ToString("X2"));
                        }
                    }
                    nPtr += nSize;

                    nSize = 1;
                    pByte = BitConverter.GetBytes(clsCamera[j].Perspective);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    if (bCompatibleData == false)
                        sb.Append(clsCamera[j].Perspective.ToString());
                    else
                        sb.Append(clsCamera[j].Perspective.ToString("X2"));
                    nPtr += nSize;

                    list.Add(sb.ToString());
                }

                // Light
                nSize = 4;
                pByte = BitConverter.GetBytes(uiLightCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiLightCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiLightCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsLight[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsLight[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    for (int k = 0; k < 3; k++)
                    {
                        nSize = 4;
                        pByte = BitConverter.GetBytes(clsLight[j].RGB[k]);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        sb.Append(clsLight[j].RGB[k].ToString() + ",");
                        nPtr += nSize;
                    }
                    for (int k = 0; k < 3; k++)
                    {
                        nSize = 4;
                        pByte = BitConverter.GetBytes(clsLight[j].Location[k]);
                        for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                        sb.Append(clsLight[j].Location[k].ToString() + ((k < 2) ? "," : ""));
                        nPtr += nSize;
                    }

                    list.Add(sb.ToString());
                }

                // SelfShadow
                nSize = 4;
                pByte = BitConverter.GetBytes(uiSelfShadowCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiSelfShadowCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiSelfShadowCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsSelfShadow[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsSelfShadow[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    nSize = 1;
                    pByte = BitConverter.GetBytes(clsSelfShadow[j].Mode);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsSelfShadow[j].Mode.ToString() + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsSelfShadow[j].Distance);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    if (bCompatibleData == false)
                    {
                        sb.Append(clsSelfShadow[j].Distance.ToString() + ((i < nSize - 1) ? "," : ""));
                    }
                    else
                    {
                        // VMD Convertert (Graphical) 1.1 ではセルフシャドウの書き出しは無いようだが、あるとしたらたぶん
                        // VMD内の数値ではなく、MMDで表示される数値になるように気がするので
                        sb.Append(((0.1F - clsSelfShadow[j].Distance) / 0.00001F).ToString() + ((i < nSize - 1) ? "," : ""));
                    }
                    nPtr += nSize;
                    // (Dsitance - 0.1) / 0.00001
                    list.Add(sb.ToString());
                }

                // IK
                nSize = 4;
                pByte = BitConverter.GetBytes(uiShowIKCount);
                for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                list.Add(uiShowIKCount.ToString());
                nPtr += nSize;

                for (j = 0; j < uiShowIKCount; j++)
                {
                    sb = new StringBuilder();

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsShowIK[j].FrameNo);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsShowIK[j].FrameNo.ToString() + ",");
                    nPtr += nSize;

                    nSize = 1;
                    pByte = BitConverter.GetBytes(clsShowIK[j].show);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsShowIK[j].show.ToString() + ",");
                    nPtr += nSize;

                    nSize = 4;
                    pByte = BitConverter.GetBytes(clsShowIK[j].ik_count);
                    for (i = 0; i < nSize; i++) buffer[nPtr + i] = pByte[i];
                    sb.Append(clsShowIK[j].ik_count.ToString() + ",");
                    nPtr += nSize;

                    for (int k = 0; k < clsShowIK[j].ik_count; k++)
                    {
                        nSize = 20;
                        for (i = 0; i < nSize; i++)
                        {
                            buffer[nPtr + i] = clsShowIK[j].ik[k].name[i];
                        }
                        sb.Append(Encoding.GetEncoding("shift_jis").GetString(clsShowIK[j].ik[k].name, 0, GetLength(clsShowIK[j].ik[k].name)) + ",");
                        nPtr += nSize;

                        nSize = 1;
                        pByte = BitConverter.GetBytes(clsShowIK[j].ik[k].on_off);
                        for (i = 0; i < nSize; i++)
                        {
                            buffer[nPtr + i] = pByte[i];
                        }
                        sb.Append(clsShowIK[j].ik[k].on_off.ToString() + ((k < clsShowIK[j].ik_count - 1) ? "," : ""));
                        nPtr += nSize;
                    }

                    list.Add(sb.ToString());
                }

                fs.Write(buffer, 0, buffer.Length);
            }
        }
        catch(Exception e)
        {
            System.Windows.Forms.MessageBox.Show(e.Message, "Error");
            return false;
        }
        finally
        {

        }

        return true;
    }

    public bool Load(string sFile)
    {
        byte[] buffer = null;
        try
        {
            using (var fs = new FileStream(sFile, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
            }
        }
        catch
        {
            return false;
        }
        finally
        {

        }

        int nPtr = 0, i = 0, nLoop = 0, j = 0;

        // VMD_HEADER
        nLoop = 30;
        for (i = 0; i < nLoop; i++)
        {
            clsHeader.VmdHeader[i] = buffer[nPtr + i];
            if (buffer[nPtr + i] == '\0') break;
        }
        clsHeader.sVmdHeader = Encoding.GetEncoding("shift_jis").GetString(clsHeader.VmdHeader, 0, i);
        nPtr += nLoop;

        nLoop = 20;
        for (i = 0; i < nLoop; i++)
        {
            clsHeader.VmdModelName[i] = buffer[nPtr + i];
            if (buffer[nPtr + i] == '\0') break;
        }
        clsHeader.sVmdModelName = Encoding.GetEncoding("shift_jis").GetString(clsHeader.VmdModelName, 0, i);
        nPtr += nLoop;

        // Motion
        nLoop = 4;
        uiMotionCount = BitConverter.ToUInt32(buffer, nPtr);
        nPtr += nLoop;

        if (uiMotionCount > 0)
        {
            clsMotion = new VMD_MOTION[uiMotionCount];
            for (i = 0; i < (int)uiMotionCount; i++)
            {
                clsMotion[i] = new VMD_MOTION();
                nLoop = 15;
                for (j = 0; j < nLoop; j++)
                {
                    clsMotion[i].BoneName[j] = buffer[nPtr + j];
                    if (buffer[nPtr + j] == '\0') break;
                }
                clsMotion[i].sBoneName = Encoding.GetEncoding("shift_jis").GetString(clsMotion[i].BoneName, 0, j);
                nPtr += nLoop;

                nLoop = 4;
                clsMotion[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsMotion[i].Location[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsMotion[i].Location[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsMotion[i].Location[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsMotion[i].Rotatation[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsMotion[i].Rotatation[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsMotion[i].Rotatation[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsMotion[i].Rotatation[3] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 64;
                for (j = 0; j < nLoop; j++)
                {
                    clsMotion[i].Interpolation[j] = buffer[nPtr + j];
                }
                nPtr += nLoop;
            }
        }

        // Skin
        nLoop = 4;
        uiSkinCount = BitConverter.ToUInt32(buffer, nPtr);
        nPtr += nLoop;

        if (uiSkinCount > 0)
        {
            clsSkin = new VMD_SKIN[uiSkinCount];
            for (i = 0; i < (int)uiSkinCount; i++)
            {
                clsSkin[i] = new VMD_SKIN();
                nLoop = 15;
                for (j = 0; j < nLoop; j++)
                {
                    clsSkin[i].SkinName[j] = buffer[nPtr + j];
                    if (buffer[nPtr + j] == '\0') break;
                }
                clsSkin[i].sSkinName = Encoding.GetEncoding("shift_jis").GetString(clsSkin[i].SkinName, 0, j);
                nPtr += nLoop;

                nLoop = 4;
                clsSkin[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsSkin[i].Weight = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
            }
        }

        // Camera
        nLoop = 4;
        uiCameraCount = BitConverter.ToUInt32(buffer, nPtr);
        nPtr += nLoop;

        if (uiCameraCount > 0)
        {
            clsCamera = new VMD_CAMERA[uiCameraCount];
            for (i = 0; i < (int)uiCameraCount; i++)
            {
                clsCamera[i] = new VMD_CAMERA();
                nLoop = 4;
                clsCamera[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsCamera[i].Length = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsCamera[i].Location[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsCamera[i].Location[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsCamera[i].Location[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsCamera[i].Rotation[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsCamera[i].Rotation[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsCamera[i].Rotation[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 24;
                for (j = 0; j < nLoop; j++)
                {
                    clsCamera[i].Interpolation[j] = buffer[nPtr + j];
                }
                nPtr += nLoop;

                nLoop = 4;
                clsCamera[i].ViewingAngle = BitConverter.ToUInt32(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 1;
                clsCamera[i].Perspective = buffer[nPtr];
                nPtr += nLoop;
            }
        }

        // Light
        nLoop = 4;
        uiLightCount = BitConverter.ToUInt32(buffer, nPtr);
        nPtr += nLoop;

        if (uiLightCount > 0)
        {
            clsLight = new VMD_LIGHT[uiLightCount];
            for (i = 0; i < (int)uiLightCount; i++)
            {
                clsLight[i] = new VMD_LIGHT();

                nLoop = 4;
                clsLight[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsLight[i].RGB[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsLight[i].RGB[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsLight[i].RGB[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;

                nLoop = 4;
                clsLight[i].Location[0] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsLight[i].Location[1] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
                nLoop = 4;
                clsLight[i].Location[2] = BitConverter.ToSingle(buffer, nPtr);
                nPtr += nLoop;
            }
        }

        // ここからはあるかどうかわからんからな...
        // SelfShadow
        uiSelfShadowCount = 0;
        if (nPtr < buffer.Length)
        {
            nLoop = 4;
            uiSelfShadowCount = BitConverter.ToUInt32(buffer, nPtr);
            nPtr += nLoop;

            if (uiSelfShadowCount > 0)
            {
                clsSelfShadow = new VMD_SELF_SHADOW[uiSelfShadowCount];
                for (i = 0; i < (int)uiSelfShadowCount; i++)
                {
                    clsSelfShadow[i] = new VMD_SELF_SHADOW();

                    nLoop = 4;
                    clsSelfShadow[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                    nPtr += nLoop;

                    nLoop = 1;
                    clsSelfShadow[i].Mode = buffer[nPtr];
                    nPtr += nLoop;

                    nLoop = 4;
                    clsSelfShadow[i].Distance = BitConverter.ToSingle(buffer, nPtr);
                    nPtr += nLoop;
                }
            }
        }

        // IK
        uiShowIKCount = 0;
        if (nPtr < buffer.Length)
        {
            nLoop = 4;
            uiShowIKCount = BitConverter.ToUInt32(buffer, nPtr);
            nPtr += nLoop;

            if (uiShowIKCount > 0)
            {
                clsShowIK = new VMD_SHOWIKFRAME[uiShowIKCount];
                for (i = 0; i < (int)uiShowIKCount; i++)
                {
                    clsShowIK[i] = new VMD_SHOWIKFRAME();

                    nLoop = 4;
                    clsShowIK[i].FrameNo = BitConverter.ToUInt32(buffer, nPtr);
                    nPtr += nLoop;

                    nLoop = 1;
                    clsShowIK[i].show = buffer[nPtr];
                    nPtr += nLoop;

                    nLoop = 4;
                    clsShowIK[i].ik_count = BitConverter.ToUInt32(buffer, nPtr);
                    nPtr += nLoop;

                    if (clsShowIK[i].ik_count > 0)
                    {
                        clsShowIK[i].ik = new VMD_InfoIK[clsShowIK[i].ik_count];
                        for (int k = 0; k < clsShowIK[i].ik_count; k++)
                        {
                            clsShowIK[i].ik[k] = new VMD_InfoIK();
                            nLoop = 20;
                            for (j = 0; j < nLoop; j++)
                            {
                                clsShowIK[i].ik[k].name[j] = buffer[nPtr + j];
                                if (buffer[nPtr + j] == '\0') break;
                            }
                            nPtr += nLoop;
                            string ss = Encoding.GetEncoding("shift_jis").GetString(clsShowIK[i].ik[k].name);

                            nLoop = 1;
                            clsShowIK[i].ik[k].on_off = buffer[nPtr];
                            nPtr += nLoop;
                        }
                    }
                }
            }
        }

        return true;
    }

}

// ヘッダ
public class VMD_HEADER
{
    public byte[] VmdHeader = new byte[30]; // char[30] "Vocaloid Motion Data 0002"
    public byte[] VmdModelName = new byte[20]; // char[20] カメラの場合:"カメラ・照明" // カメラ・照明・アクセサリモードではモデル用のVMDは読めなくなりました(7.10-)

    public string sVmdHeader;
    public string sVmdModelName;
}

// モーションデータ
public class VMD_MOTION
{ // 111 Bytes // モーション
    public byte[] BoneName = new byte[15]; // char[15] ボーン名
    public uint FrameNo; // DWORD フレーム番号(読込時は現在のフレーム位置を0とした相対位置)
    public float[] Location = new float[3]; // float[3] 位置
    public float[] Rotatation = new float[4]; // float[4] Quaternion // 回転
    public byte[] Interpolation = new byte[64]; // BYTE[64] [4][4][4] // 補完

    public string sBoneName;
}

// 表情データ
public class VMD_SKIN
{ // 23 Bytes // 表情
    public byte[] SkinName = new byte[15]; // char[15] 表情名
    public uint FrameNo; // DWORD フレーム番号
    public float Weight; // float 表情の設定値(表情スライダーの値)

    public string sSkinName;
}

// カメラデータ
public class VMD_CAMERA
{ // 61 Bytes // カメラ
    public uint FrameNo; // DWORD フレーム番号
    public float Length; // float -(距離)
    public float[] Location = new float[3]; // float[3] 位置
    public float[] Rotation = new float[3]; // float[3] オイラー角 // X軸は符号が反転しているので注意 // 回転
    public byte[] Interpolation = new byte[24]; // BYTE[24] おそらく[6][4](未検証) // 補完
    public uint ViewingAngle; // DWORD 視界角
    public byte Perspective; // BYTE 0:on 1:off // パースペクティブ
}

// 照明データ
public class VMD_LIGHT
{ // 28 Bytes // 照明
    public uint FrameNo; // DWORD フレーム番号
    public float[] RGB = new float[3]; // float[3] RGB各値/256 // 赤、緑、青
    public float[] Location = new float[3]; // float[3]  X, Y, Z
}

// セルフシャドウデータ
public class VMD_SELF_SHADOW
{ // 9 Bytes // セルフシャドー
    public uint FrameNo; // DWORD フレーム番号
    public byte Mode; // BYTE 00-02 // モード
    public float Distance; // float 0.1 - (dist * 0.00001) // 距離
}

//モデル表示・IK on/offキーフレーム要素データ((9+21*IK数)Bytes/要素)
public class VMD_SHOWIKFRAME
{
    public uint FrameNo; // DWORD フレーム番号
    public byte show; // char モデル表示, 0:OFF, 1:ON
    public uint ik_count; // DWORD 記録するIKの数
    public VMD_InfoIK[] ik; // ik[iK_count]  IK on/off情報配列
};

public class VMD_InfoIK
{
    public byte[] name = new byte[20]; // char[20] "右足ＩＫ\0"などのIKボーン名の文字列 20byte
    public byte on_off; // char IKのon/off, 0:OFF, 1:ON
};
