/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

/*
 * Copyright 2023 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace LibreccioWeather.Core.MaterialColors;

/// <summary>
/// A required distance between the tones of two colors that are not text and background, such as a container and
/// the color beside it.
/// </summary>
internal sealed class ToneDeltaPair
{
    /// <summary>
    /// How the distance is met.
    /// </summary>
    public enum DeltaConstraint
    {
        Exact,
        Nearer,
        Farther,
    }

    /// <summary>
    /// A distance that the first role's tone keeps from the second's, in the direction of the polarity. The relative
    /// polarities follow the surfaces: lighter in a light scheme means darker in a dark one.
    /// </summary>
    /// <param name="stayTogether">Whether both roles stay on the same side of the awkward tones 50 to 59, for a
    /// role that has two backgrounds.</param>
    public ToneDeltaPair(DynamicColor roleA, DynamicColor roleB, double delta, TonePolarity polarity, bool stayTogether)
    {
        RoleA = roleA;
        RoleB = roleB;
        Delta = delta;
        Polarity = polarity;
        StayTogether = stayTogether;
        Constraint = DeltaConstraint.Exact;
    }

    public ToneDeltaPair(DynamicColor roleA, DynamicColor roleB, double delta, TonePolarity polarity, DeltaConstraint constraint)
    {
        RoleA = roleA;
        RoleB = roleB;
        Delta = delta;
        Polarity = polarity;
        StayTogether = true;
        Constraint = constraint;
    }

    public DynamicColor RoleA { get; }

    public DynamicColor RoleB { get; }

    /// <summary>
    /// The distance between the tones; a negative distance is not defined.
    /// </summary>
    public double Delta { get; }

    public TonePolarity Polarity { get; }

    public bool StayTogether { get; }

    public DeltaConstraint Constraint { get; }
}

/// <summary>
/// Which way a tone is pushed from another one.
/// </summary>
internal enum TonePolarity
{
    Darker,
    Lighter,
    RelativeDarker,
    RelativeLighter,
    Nearer,
    Farther,
}
