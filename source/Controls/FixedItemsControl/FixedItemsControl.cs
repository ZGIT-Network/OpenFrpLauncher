using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace OpenFrp.Launcher.Controls
{
    public class FixedItemsControl : ItemsControl
    {
        // https://blog.walterlv.com/post/wpf-items-control-supports-ui-automation


        public List<GroupItemAutomationPeer> GetGroupItemAutomationPeers()
        {
            if (Items.GroupDescriptions.Count is > 0)
            {
                var selfPeer = OnCreateAutomationPeer() as ItemsControlWrapperAutomationPeer;

                List<GroupItemAutomationPeer> peers = new List<GroupItemAutomationPeer>();

                if (selfPeer != null && selfPeer.GetChildren() is { Count: > 0 } peersChildren && peersChildren.All(p => p is GroupItemAutomationPeer))
                {
                    return peersChildren.OfType<GroupItemAutomationPeer>().ToList();
                }
            }

            return new List<GroupItemAutomationPeer> { };
        }

        public void InvaildateAutomationPeers()
        {
            var peer = UIElementAutomationPeer.FromElement(this) as ItemsControlWrapperAutomationPeer;
            peer?.ResetChildrenCache();
            peer?.InvalidatePeer();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ItemsControlWrapperAutomationPeer(this);
        }

        private sealed class ItemsControlWrapperAutomationPeer : ItemsControlAutomationPeer
        {
            public ItemsControlWrapperAutomationPeer(ItemsControl owner) : base(owner)
            {
            }

            protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
            {
                return new ItemsControlItemAutomationPeer(item, this);
            }

            protected override string GetClassNameCore()
            {
                return "ItemsControl";
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.List;
            }
        }

        private class ItemsControlItemAutomationPeer : ItemAutomationPeer
        {
            public ItemsControlItemAutomationPeer(object item, ItemsControlWrapperAutomationPeer parent)
                : base(item, parent)
            { }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.DataItem;
            }

            protected override string GetClassNameCore()
            {
                return "ItemsControlItem";
            }
        }
    }
}
