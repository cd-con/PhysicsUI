using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace BoredAF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PhysicsProcessor physicsProcessor;
        public MainWindow()
        {
            InitializeComponent();

            physicsProcessor = new PhysicsProcessor(this);

            System.Timers.Timer timer = new System.Timers.Timer(16);

            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;

            physicsProcessor.Add(PhysicsButton);
            physicsProcessor.Add(Ground);

            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() => { 
                    physicsProcessor.Tick(0.016f);
                    InvalidateVisual();
                });

            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message, "Physics thread error!");
            }
        }
    }

    internal class PhysicsProcessor
    {
        private const float g_accel = .981f;
        private Window _ctx;
        private List<PhysicsFrameworkElement> physicsItems = new List<PhysicsFrameworkElement>();

        public PhysicsProcessor(Window context) => _ctx = context;

        public PhysicsFrameworkElement Add(FrameworkElement element)
        {
            PhysicsFrameworkElement elem = new PhysicsFrameworkElement(_ctx, element);

            physicsItems.Add(elem);

            return elem;
        }

        public void Remove(PhysicsFrameworkElement elem)
        {
            physicsItems.Remove(elem);
        }

        public void Tick(float frameTime)
        {
            foreach (PhysicsFrameworkElement item in physicsItems)
            {
                Point accel = item.GetVelocity();
                Point pos = item.GetPosition();

                if (!item.isStatic)
                {
                    accel.Y *= -g_accel;
                    item.SetVelocity(accel.Mul(frameTime));
                }


                foreach (PhysicsFrameworkElement overlapping in GetOverlapping(item))
                {
                    Point overlAccel = overlapping.GetVelocity();
                    Point pushVel = (Point)(overlAccel - accel);

                    overlapping.SetVelocity(overlAccel.Add(pushVel.Mul(overlapping.friction)));
                    item.SetVelocity(accel.Add(pushVel.Mul(item.friction)));
                }

                item.SetPosition(pos);
            }
        }

        private IEnumerable<PhysicsFrameworkElement> GetOverlapping(PhysicsFrameworkElement element)
        {
            for (int i = 0; i < physicsItems.Count; i++)
            {
                PhysicsFrameworkElement item = physicsItems[i];

                if (item == element)
                    continue;

                if (element.IsOverlapping(item))
                    yield return item;
            }
        }
    }

    internal class PhysicsFrameworkElement
    {
        public float bounciness = 0.5f;
        public float friction = 0.33f;
        public bool isStatic = false;

        private Point acceleration = new Point(0, 0);
        private FrameworkElement _e;
        private FrameworkElement _ctx;

        public PhysicsFrameworkElement(Window ctx, FrameworkElement element)
        {
            _e = element;
            _ctx = ctx;
        }

        public Point GetPosition() => _e.TransformToAncestor(_ctx).Transform(new Point(0, 0));
        public void SetPosition(Point newPos)
        {
            _e.TranslatePoint(newPos, _ctx);
        }

        public Point GetVelocity() => acceleration;

        public void SetVelocity(Point newVel)
        {
            if (isStatic)
                return;
            
            acceleration = newVel;
        }

        public Rect GetRect()
        {
            Point pos = GetPosition();
            return new Rect(pos.X, pos.Y, _e.Width, _e.Height);
        }

        public bool IsOverlapping(PhysicsFrameworkElement other) => GetRect().Intersects(other.GetRect());
    }

    public static class Utility
    {
        public static Point Mul(this Point A, float B) => new Point(A.X * B, A.Y * B);
        public static Point Add(this Point A, Point B) => new Point(A.X + B.X, A.Y + B.Y);
        public static bool Intersects(this Rect A, Rect B) => A.Left < B.Right && A.Right > B.Left && A.Top < B.Bottom && A.Bottom > B.Top;
    }
}

