/**
 * Project: emergetk: stateful web framework for the masses
 * File name: sliderTester.cs
 * Description: tests the slider widget.
 *   
 * @author Ben Joldersma, All-In-One Creations, Ltd. http://all-in-one-creations.net, Copyright (C) 2006.
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Tests
{
    public class SliderTest : Context
    {
        public override void Initialize()
        {
            Slider s = CreateWidget<Slider>();
            s.Min = 0;
            s.Max = 100;
            TextBox tb = CreateWidget<TextBox>();
            s.Bind(tb);
            Add(s, tb);

            s = CreateWidget<Slider>();
            s.Min = 200;
            s.Max = 255;
            s.Orientation = Orientation.Vertical;
            tb = CreateWidget<TextBox>();
            s.Bind(tb);
            Add(s, tb);

        }
    }
}
