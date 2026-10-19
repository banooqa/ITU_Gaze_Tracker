// <copyright file="CalibrationResultControl.cs" company="FU Berlin">
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
	using System.Drawing;
	using System.Windows.Forms;

	/// <summary>
	/// This WinForms <see cref="UserControl"/> can be integrated into
	/// OGAMAs recording modul to display the calibration result
	/// of the ITU GazeTracker.
	/// </summary>
	public partial class CalibrationResultControl : UserControl
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
		#endregion //FIELDS

		///////////////////////////////////////////////////////////////////////////////
		// Construction and Initializing methods                                     //
		///////////////////////////////////////////////////////////////////////////////
		#region CONSTRUCTION

		/// <summary>
		/// Initializes a new instance of the CalibrationResultControl class.
		/// </summary>
		public CalibrationResultControl()
		{
			this.InitializeComponent();
		}

		#endregion //CONSTRUCTION

		///////////////////////////////////////////////////////////////////////////////
		// Defining events, enums, delegates                                         //
		///////////////////////////////////////////////////////////////////////////////
		#region EVENTS
		#endregion EVENTS

		///////////////////////////////////////////////////////////////////////////////
		// Defining Properties                                                       //
		///////////////////////////////////////////////////////////////////////////////
		#region PROPERTIES

		/// <summary>
		/// Sets the <see cref="Image"/> with the calibration result.
		/// </summary>
		public Image CalibrationResult
		{
			set { this.pcbResult.Image = value; }
		}

		/// <summary>
		/// Sets the rating result for the calibration quality (1-5)
		/// </summary>
		public int CalibrationResultRating
		{
			set { this.starRating.Rating = value; }
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
		#endregion //PRIVATEMETHODS

		///////////////////////////////////////////////////////////////////////////////
		// Small helping Methods                                                     //
		///////////////////////////////////////////////////////////////////////////////
		#region HELPER
		#endregion //HELPER
	}
}
