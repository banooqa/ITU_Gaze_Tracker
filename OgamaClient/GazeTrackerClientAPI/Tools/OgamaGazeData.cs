// <copyright file="OgamaGazeData.cs" company="FU Berlin">
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
  using System.Collections.Generic;
  using System.Text;

  /// <summary>
  /// Gaze data structure with fields that match the database columns
  /// that correspond to gaze data. Its a subset of OGAMAs Modules.ImportExport.RawData.
  /// </summary>
  public struct OgamaGazeData
  {
    /// <summary>
    /// Time in milliseconds from the start of the recording.
    /// </summary>
    public long Time;

    /// <summary>
    /// x-diameter of pupil
    /// </summary>
    public float? PupilDiaX;

    /// <summary>
    /// y-diameter of pupil
    /// </summary>
    public float? PupilDiaY;

    /// <summary>
    /// x-coordinate of gaze position in values ranging between 0..1
    /// </summary>
    /// <remarks>0 means left margin of presentation screen,
    /// 1 means right margin of presentation screen.</remarks>
    public float? GazePosX;

    /// <summary>
    /// y-coordinate of gaze position in values ranging between 0..1
    /// </summary>
    /// <remarks>0 means top margin of presentation screen,
    /// 1 means bottom margin of presentation screen.</remarks>
    public float? GazePosY;
  }
}
