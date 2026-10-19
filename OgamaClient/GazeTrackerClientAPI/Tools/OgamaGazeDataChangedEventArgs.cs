// <copyright file="OgamaGazeDataChangedEventArgs.cs" company="FU Berlin">
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

  /// <summary>
  /// Delegate. Handles gaze data changed event.
  /// </summary>
  /// <param name="sender">Source of the event.</param>
  /// <param name="e">A <see cref="GazeDataChangedEventArgs"/> with the new gaze data.</param>
  public delegate void OgamaGazeDataChangedEventHandler(object sender, OgamaGazeDataChangedEventArgs e);

  /// <summary>
  /// Derived from <see cref="System.EventArgs"/>
  /// Class that contains the data for the gaze data changed event. 
  /// </summary>
  public class OgamaGazeDataChangedEventArgs : EventArgs
  {
    ///////////////////////////////////////////////////////////////////////////////
    // Defining Variables, Enumerations, Events                                  //
    ///////////////////////////////////////////////////////////////////////////////
    #region FIELDS
    /// <summary>
    /// The new gaze data.
    /// </summary>
    private readonly OgamaGazeData gazedata;

    #endregion //FIELDS

    ///////////////////////////////////////////////////////////////////////////////
    // Construction and Initializing methods                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region CONSTRUCTION

    /// <summary>
    /// Initializes a new instance of the OgamaGazeDataChangedEventArgs class.
    /// </summary>
    /// <param name="newGazedata">The gaze data as a <see cref="GazeData"/> value.</param>
    public OgamaGazeDataChangedEventArgs(OgamaGazeData newGazedata)
    {
      this.gazedata = newGazedata;
    }

    #endregion //CONSTRUCTION

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Properties                                                       //
    ///////////////////////////////////////////////////////////////////////////////
    #region PROPERTIES
    /// <summary>
    /// Gets the new gaze data.
    /// </summary>
    /// <value>The gaze data as a <see cref="OgamaGazeData"/> value.</value>
    public OgamaGazeData Gazedata
    {
      get { return this.gazedata; }
    }
    #endregion //PROPERTIES
  }
}
