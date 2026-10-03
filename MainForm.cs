using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorRotator;

public class MainForm : Form
{
    public MainForm()
    {
        // Keep the form invisible.
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;

        Load += MainForm_Load;
    }

    private void MainForm_Load(
        object? sender,
        EventArgs e)
    {
        try
        {
            DisplayManager.RotateExternalMonitor();

            // Rotation completed successfully.
            Close();
        }
        catch (Exception ex)
        {
            // Show an error only if something goes wrong.
            Opacity = 1;
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;

            MessageBox.Show(
                ex.Message,
                "Monitor Rotator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Close();
        }
    }
}