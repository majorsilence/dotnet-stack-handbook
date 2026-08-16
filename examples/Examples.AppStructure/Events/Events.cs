// The chapter shows two classes both called TheExample, one using the built in
// EventHandler and one using a custom delegate.  Separate namespaces here so both
// can exist at once.
namespace Examples.AppStructure.Events.BuiltIn
{
    public class TheExample
    {
        public event System.EventHandler DoSomething;

        public void TheTest()
        {
            // option 1 to raise event
            this.DoSomething?.Invoke(this, new System.EventArgs());

            // option 2 to raise event
            if (DoSomething != null)
            {
                DoSomething(this, new System.EventArgs());
            }
        }
    }
}

namespace Examples.AppStructure.Events.CustomDelegate
{
    public class TheExample
    {
        public delegate void MyCustomEventHandler(object sender, System.EventArgs e);
        public event MyCustomEventHandler DoSomething;

        public void TheTest()
        {
            // option 1 to raise event
            this.DoSomething?.Invoke(this, new System.EventArgs());

            // option 2 to raise event
            if (DoSomething != null)
            {
                DoSomething(this, new System.EventArgs());
            }
        }
    }
}

namespace Examples.AppStructure.Events.Custom
{
    public delegate void MyCustomEventHandler(object sender, MyCustomEvent e);

    public class MyCustomEvent : System.EventArgs
    {
        private string _msg;
        private float _value;

        public MyCustomEvent(string m)
        {
            _msg = m;
            _value = 0;
        }

        public MyCustomEvent(float v)
        {
            _msg = "";
            _value = v;
        }

        public string Message
        {
            get { return _msg; }
        }

        public float Value
        {
            get { return _value; }
        }
    }

    public class Publisher
    {
        public event MyCustomEventHandler DoSomething;

        public void Raise()
        {
            this.DoSomething?.Invoke(this, new MyCustomEvent(123.95f));
        }
    }
}
