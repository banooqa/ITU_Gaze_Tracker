// <copyright file="StarRating.cs" company="FU Berlin">
// ******************************************************
// OgamaClient for ITU GazeTracker
// Copyright (C) 2010 Adrian Voßkühler  
// ------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it 
// under the terms of the GNU General Public License as published by the 
// Free Software Foundation; either version 3 of the License, 
// or (at your option) any later version.
// This program is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
// General Public License for more details.
// You should have received a copy of the GNU General Public License 
// along with this program; if not, see http://www.gnu.org/licenses/.
// **************************************************************
// </copyright>
// <author>Adrian Voßkühler</author>
// <email>adrian.vosskuehler@fu-berlin.de</email>

namespace OgamaClient
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;

	/// <summary>
	/// This is a win forms <see cref="UserControl"/> to display a five star rating
	/// which indicates good or bad calibration quality.
	/// </summary>
	public partial class StarRating : UserControl
	{
		///////////////////////////////////////////////////////////////////////////////
		// Defining Constants                                                        //
		///////////////////////////////////////////////////////////////////////////////
		#region CONSTANTS
		#endregion //CONSTANTS

		///////////////////////////////////////////////////////////////////////////////
		// Defining Variables, Enumerations, Events                                  //
		///////////////////////////////////////////////////////////////////////////////
		#region FIELDS

		/// <summary>
		/// Saves the current <see cref="Layouts"/>
		/// </summary>
		private Layouts controlLayout;

		/// <summary>
		/// Saves the current rating value.
		/// </summary>
		private int rating;

		/// <summary>
		/// Saves the wrapperPanelBorderStyle-
		/// </summary>
		private BorderStyle wrapperPanelBorderStyle;

		#endregion //FIELDS

		///////////////////////////////////////////////////////////////////////////////
		// Construction and Initializing methods                                     //
		///////////////////////////////////////////////////////////////////////////////
		#region CONSTRUCTION

		/// <summary>
		/// Initializes a new instance of the StarRating class.
		/// </summary>
		public StarRating()
		{
			this.InitializeComponent();
			this.controlLayout = Layouts.Horizontal;
			this.rating = 0;
			this.wrapperPanelBorderStyle = BorderStyle.None;
		}

		#endregion //CONSTRUCTION

		///////////////////////////////////////////////////////////////////////////////
		// Defining events, enums, delegates                                         //
		///////////////////////////////////////////////////////////////////////////////
		#region EVENTS

		/// <summary>
		/// This enumeration describes possible layouts for this
		/// star rating control, which are horizontal or vertical.
		/// </summary>
		public enum Layouts
		{
			/// <summary>
			/// The five star rating is layed out horizontal.
			/// </summary>
			Horizontal,

			/// <summary>
			/// The five star rating is layed out vertical.
			/// </summary>
			Vertical
		}

		#endregion EVENTS

		///////////////////////////////////////////////////////////////////////////////
		// Defining Properties                                                       //
		///////////////////////////////////////////////////////////////////////////////
		#region PROPERTIES

		/// <summary>
		/// Gets or sets the controls layout.
		/// </summary>
		public Layouts ControlLayout
		{
			get
			{
				return this.controlLayout;
			}

			set
			{
				this.controlLayout = value;
				this.OrientControl();
			}
		}

		/// <summary>
		/// Gets or sets the rating value for the control. (1-5).
		/// </summary>
		public int Rating
		{
			get
			{
				return this.rating;
			}

			set
			{
				this.rating = value;
				this.ShowRating();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="BorderStyle"/>
		/// for the controls wrapper.
		/// </summary>
		public BorderStyle WrapperPanelBorderStyle
		{
			get
			{
				return this.BorderStyle;
			}

			set
			{
				this.wrapperPanelBorderStyle = value;
				this.BorderStyle = value;
			}
		}

		#endregion //PROPERTIES

		///////////////////////////////////////////////////////////////////////////////
		// Public methods                                                            //
		///////////////////////////////////////////////////////////////////////////////
		#region PUBLICMETHODS
		#endregion //PUBLICMETHODS

		///////////////////////////////////////////////////////////////////////////////
		// Inherited methods                                                         //
		///////////////////////////////////////////////////////////////////////////////
		#region OVERRIDES
		#endregion //OVERRIDES

		///////////////////////////////////////////////////////////////////////////////
		// Eventhandler                                                              //
		///////////////////////////////////////////////////////////////////////////////
		#region EVENTHANDLER

		/// <summary>
		/// The <see cref="UserControl.Load"/> event handler
		/// which initializes orientation of the control.
		/// </summary>
		/// <param name="sender">Source of the event</param>
		/// <param name="e">An empty EventArgs.</param>
		private void StarRating_Load(object sender, EventArgs e)
		{
			this.OrientControl();
			this.ShowRating();
		}

		#endregion //EVENTHANDLER

		///////////////////////////////////////////////////////////////////////////////
		// Methods and Eventhandling for Background tasks                            //
		///////////////////////////////////////////////////////////////////////////////
		#region THREAD
		#endregion //THREAD

		///////////////////////////////////////////////////////////////////////////////
		// Methods for doing main class job                                          //
		///////////////////////////////////////////////////////////////////////////////
		#region PRIVATEMETHODS

		/// <summary>
		/// This method orients the controls items
		/// according to the <see cref="ControlLayout"/> property.
		/// </summary>
		private void OrientControl()
		{
			switch (this.ControlLayout)
			{
				case Layouts.Vertical:
					this.Size = new Size(22, 88);
					this.pbStar1.Location = new Point(2, 66);
					this.pbStar2.Location = new Point(2, 50);
					this.pbStar3.Location = new Point(2, 34);
					this.pbStar4.Location = new Point(2, 18);
					this.pbStar5.Location = new Point(2, 2);
					break;
				case Layouts.Horizontal:
					this.Size = new Size(88, 22);
					this.pbStar1.Location = new Point(0, 1);
					this.pbStar2.Location = new Point(17, 1);
					this.pbStar3.Location = new Point(34, 1);
					this.pbStar4.Location = new Point(51, 1);
					this.pbStar5.Location = new Point(68, 1);
					break;
			}
		}

		/// <summary>
		/// This method displays the current ranking
		/// by graying out or displaying the five starts.
		/// </summary>
		private void ShowRating()
		{
			switch (this.Rating)
			{
				case 0:
					this.pbStar1.Image = Properties.Resources.rating_star_disabled;
					this.pbStar2.Image = Properties.Resources.rating_star_disabled;
					this.pbStar3.Image = Properties.Resources.rating_star_disabled;
					this.pbStar4.Image = Properties.Resources.rating_star_disabled;
					this.pbStar5.Image = Properties.Resources.rating_star_disabled;
					break;

				case 1:
					this.pbStar1.Image = Properties.Resources.rating_star_enabled;
					this.pbStar2.Image = Properties.Resources.rating_star_disabled;
					this.pbStar3.Image = Properties.Resources.rating_star_disabled;
					this.pbStar4.Image = Properties.Resources.rating_star_disabled;
					this.pbStar5.Image = Properties.Resources.rating_star_disabled;
					break;

				case 2:
					this.pbStar1.Image = Properties.Resources.rating_star_enabled;
					this.pbStar2.Image = Properties.Resources.rating_star_enabled;
					this.pbStar3.Image = Properties.Resources.rating_star_disabled;
					this.pbStar4.Image = Properties.Resources.rating_star_disabled;
					this.pbStar5.Image = Properties.Resources.rating_star_disabled;
					break;

				case 3:
					this.pbStar1.Image = Properties.Resources.rating_star_enabled;
					this.pbStar2.Image = Properties.Resources.rating_star_enabled;
					this.pbStar3.Image = Properties.Resources.rating_star_enabled;
					this.pbStar4.Image = Properties.Resources.rating_star_disabled;
					this.pbStar5.Image = Properties.Resources.rating_star_disabled;
					break;

				case 4:
					this.pbStar1.Image = Properties.Resources.rating_star_enabled;
					this.pbStar2.Image = Properties.Resources.rating_star_enabled;
					this.pbStar3.Image = Properties.Resources.rating_star_enabled;
					this.pbStar4.Image = Properties.Resources.rating_star_enabled;
					this.pbStar5.Image = Properties.Resources.rating_star_disabled;
					break;

				case 5:
					this.pbStar1.Image = Properties.Resources.rating_star_enabled;
					this.pbStar2.Image = Properties.Resources.rating_star_enabled;
					this.pbStar3.Image = Properties.Resources.rating_star_enabled;
					this.pbStar4.Image = Properties.Resources.rating_star_enabled;
					this.pbStar5.Image = Properties.Resources.rating_star_enabled;
					break;
			}
		}

		#endregion //PRIVATEMETHODS

		///////////////////////////////////////////////////////////////////////////////
		// Small helping Methods                                                     //
		///////////////////////////////////////////////////////////////////////////////
		#region HELPER
		#endregion //HELPER
	}
}
