using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.AurorasAdditions.Entities
{
    class DrawModeUI : Entity
    {
        public Dictionary<Vector2, LineData> points;
        public Dictionary<Vector2[], LineData> lines;
        public Boolean drawEraserCircle = false;
        public Boolean drawCursor = false;
        public Color currColor;

        public struct LineData
        {
            public Color color;
            public int thickness;

            public LineData(Color color, int thickness)
            {
                this.color = color;
                this.thickness = thickness;
            }
        }

        public DrawModeUI()
        {
            Tag = Tags.HUD;
            base.Depth = -1000000;
            points = new Dictionary<Vector2, LineData>();
            lines = new Dictionary<Vector2[], LineData>();
            currColor = Color.White;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Player player = scene.Tracker.GetEntity<Player>();
            if(player == null)
            {
                Logger.Log(LogLevel.Debug, "Aurora's Helper", "Could not find player trying to open DrawMode UI");
                RemoveSelf();
                return;
            }

            // remove menu if it is open
            if(player?.SceneAs<Level>()?.PauseMainMenuOpen ?? false)
            {
                player.SceneAs<Level>().Entities.FindFirst<TextMenu>()?.RemoveSelf();
            }

            player.SceneAs<Level>().Paused = true;

            Audio.Play("event:/ui/game/pause");
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            Player player = scene.Tracker.GetEntity<Player>();
            if (player == null)
            {
                return;
            }

            player.SceneAs<Level>().Paused = false;
            Audio.Play("event:/ui/game/unpause");
        }

        private Boolean isInScreenBounds(Vector2 point)
        {
            return point.Y >= 0 && point.Y < Engine.Height && point.X >= 0 && point.X <= Engine.Width;
        }

        public override void Render()
        {
            base.Render();

            foreach (KeyValuePair<Vector2, LineData> kvp in points)
            {
                Vector2 point = kvp.Key;
                Color color = kvp.Value.color;
                int thickness = kvp.Value.thickness;

                if (isInScreenBounds(point))
                {
                    Draw.Point(point, color);
                }
            }
            foreach (KeyValuePair<Vector2[], LineData> kvp in lines)
            {
                Vector2[] lineArr = kvp.Key;
                Color color = kvp.Value.color;
                int thickness = kvp.Value.thickness;

                if (lineArr.Length != 2) continue;

                Vector2 start = lineArr[0];
                Vector2 end = lineArr[1];

                if(isInScreenBounds(start) && isInScreenBounds(end))
                {
                    Draw.Line(start, end, color, thickness);
                }
            }

            if (drawEraserCircle)
            {
                for(int i = 1; i <= 33; i++)
                {
                    Draw.Circle(MInput.Mouse.Position, i, Color.Red, 100);
                }
            }

            if (drawCursor)
            {
                Vector2 mousePosition = MInput.Mouse.Position;
                for (int x = -1; x <= 1; x++)
                {
                    Vector2 start = mousePosition + new Vector2(x * 7, 0);
                    Vector2 end = mousePosition + new Vector2(x * 2, 0);
                    Draw.Line(start, end, currColor, 2);
                }
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 start = mousePosition + new Vector2(0, y * 7);
                    Vector2 end = mousePosition + new Vector2(0, y * 2);
                    Draw.Line(start, end, currColor, 2);
                }
            }
        }
    }
}
