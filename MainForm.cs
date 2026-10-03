using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorRotator;

public class MainForm : Form
{
    private readonly Button rotateButton;

    public MainForm()
    {
        Text = "Monitor Rotator";

        StartPosition = FormStartPosition.CenterScreen;

        ClientSize = new Size(300, 180);

        FormBorderStyle = FormBorderStyle.FixedSingle;

        MaximizeBox = false;

        MinimizeBox = true;

        rotateButton = new Button
        {
            Text = "↻  Rotate Monitor",
            Font = new Font(
                "Segoe UI",
                14,
                FontStyle.Regular),
            Width = 220,
            Height = 60,
            Left = 40,
            Top = 50,
            TabIndex = 0
        };

        rotateButton.Click += RotateButton_Click;

        Controls.Add(rotateButton);
    }

    private void RotateButton_Click(
        object? sender,
        EventArgs e)
    {
        try
        {
            rotateButton.Enabled = false;
            rotateButton.Text = "Rotating...";

            DisplayManager.RotateExternalMonitor();

            rotateButton.Text = "↻  Rotate Monitor";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Monitor Rotator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            rotateButton.Text = "↻  Rotate Monitor";
        }
        finally
        {
            rotateButton.Enabled = true;
        }
    }
}