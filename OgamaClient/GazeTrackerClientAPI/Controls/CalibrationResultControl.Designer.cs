namespace OgamaClient
{
  partial class CalibrationResultControl
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.pcbResult = new System.Windows.Forms.PictureBox();
      this.starRating = new OgamaClient.StarRating();
      ((System.ComponentModel.ISupportInitialize)(this.pcbResult)).BeginInit();
      this.SuspendLayout();
      // 
      // pcbResult
      // 
      this.pcbResult.BackColor = System.Drawing.SystemColors.ActiveCaption;
      this.pcbResult.Dock = System.Windows.Forms.DockStyle.Fill;
      this.pcbResult.Location = new System.Drawing.Point(0, 0);
      this.pcbResult.Name = "pcbResult";
      this.pcbResult.Size = new System.Drawing.Size(195, 148);
      this.pcbResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
      this.pcbResult.TabIndex = 0;
      this.pcbResult.TabStop = false;
      // 
      // starRating
      // 
      this.starRating.BackColor = System.Drawing.Color.Silver;
      this.starRating.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.starRating.ControlLayout = OgamaClient.StarRating.Layouts.Horizontal;
      this.starRating.Location = new System.Drawing.Point(52, 57);
      this.starRating.Margin = new System.Windows.Forms.Padding(0);
      this.starRating.Name = "starRating";
      this.starRating.Padding = new System.Windows.Forms.Padding(1);
      this.starRating.Rating = 0;
      this.starRating.Size = new System.Drawing.Size(88, 22);
      this.starRating.TabIndex = 1;
      this.starRating.WrapperPanelBorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      // 
      // CalibrationResultControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.starRating);
      this.Controls.Add(this.pcbResult);
      this.Name = "CalibrationResultControl";
      this.Size = new System.Drawing.Size(195, 148);
      ((System.ComponentModel.ISupportInitialize)(this.pcbResult)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.PictureBox pcbResult;
    private StarRating starRating;

  }
}
