/** Copyright (c) 2006, All-In-One Creations, Ltd.
*  All rights reserved.
* 
* Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* 
*     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
*     * Neither the name of All-In-One Creations, Ltd. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
/**
 * Project: emergetk: stateful web framework for the masses
 * File name: .cs
 * Description:
 *   
 * Author: Ben Joldersma
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
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{	
    public class Tree : Widget, IDataSourced
    {
    	public string PropertySource { get { return null; } set {} }
        private string key;

        public string Key
        {
            get { return key; }
            set { key = value; RaisePropertyChangedNotification("Key"); }
        }

        private string parentKey;

        public string ParentKey
        {
            get { return parentKey; }
            set { parentKey = value; RaisePropertyChangedNotification("ParentKey"); }
        }
        
        private string childrenKey;
        public string ChildrenKey
        {
        	get { return childrenKey; }
        	set { childrenKey = value; RaisePropertyChangedNotification("ParentKey"); }
        }

        private string titleKey;

        public string TitleKey
        {
            get { return titleKey; }
            set { titleKey = value; RaisePropertyChangedNotification("TitleKey"); }
        }
	
	
        IRecordList dataSource;
        bool dataBound = false;
        public bool IsDataBound { get { return dataBound; } set { dataBound = value; } }

        #region IDataSourced Members

        public EmergeTk.Model.IRecordList DataSource
        {
            get
            {
                return dataSource;
            }
            set
            {
                dataSource = value;
            }
        }

		/*DataSource behavior:
		CHANGE: Nodes in the provided recordlist are now root nodes.
		Having a flat list of hierarchical data feels like a bad impl. of O/R mapping.
		We will instead walk down the Children property of each model node.
		*/
        public void DataBind()
        {
            if (dataSource == null || dataSource.Count == 0)
                return;
	       	recordToNode = new Dictionary<AbstractRecord,Widget>();
            recurseBind(this, DataSource);
            dataBound = true;
        }

		private Dictionary<AbstractRecord,Widget> recordToNode;
        private void recurseBind(Widget parent, IRecordList children)
        {
		   	children.OnRecordAdded += new EventHandler<RecordEventArgs>(recordAdded);
		   	children.OnRecordRemoved += new EventHandler<RecordEventArgs>(recordRemoved);
	       	foreach (AbstractRecord r in children)
	     	{
	     		AddChildNode( parent, r );
           	}
        }
        
        private void AddChildNode( Widget parent, AbstractRecord r )
        {
         	   TreeNode n = RootContext.CreateWidget<TreeNode>();
	           recordToNode[r] = n;		          
	           n.OnClick += new EventHandler<ClickEventArgs>(nodeSelectManager);
	           n.Title = r[TitleKey].ToString();
	           n.Record = r;
	           parent.Add(n);
	           if( r[ChildrenKey] != null )
	          	recurseBind(n,r[ChildrenKey] as IRecordList);
        }
        
        private void recordAdded(object sender, RecordEventArgs ea)
        {
        	if( recordToNode.ContainsKey(ea.Record[ParentKey] as AbstractRecord) )
        	{
        		Widget parentNode = recordToNode[ea.Record[ParentKey] as AbstractRecord];
        		AddChildNode( parentNode, ea.Record );
        	}
        }

        private void recordRemoved(object sender, RecordEventArgs ea)
        {
        	if( recordToNode.ContainsKey(ea.Record) )
        	{
        		recordToNode[ea.Record].Remove();
        	}
        }

        private TreeNode selectedNode;

        virtual public TreeNode SelectedNode
        {
            get { return selectedNode; }
            set { selectedNode = value; RaisePropertyChangedNotification("SelectedNode");}
        }
	
        public AbstractRecord Selected {
        	get {
        		throw new NotImplementedException();
        	}
        	set {
        		throw new NotImplementedException();
        	}
        }
	
        void nodeSelectManager(object sender, ClickEventArgs ea )
        {
            SelectedNode = (TreeNode)sender;
            if( OnNodeSelected != null )
            {
            	OnNodeSelected(this, new TreeNodeSelectedEventArgs( this, SelectedNode ) );
            }
        }

		public override void PostInitialize ()
		{
			InvokeClientMethod("Startup");
		}

		public event EventHandler<TreeNodeSelectedEventArgs> OnNodeSelected;

        #endregion
    }
}
