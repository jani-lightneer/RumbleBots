using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Core
{
    public class AssetLoader
    {
        private const string DIRECTORY = "Assets";

        private readonly GraphicsDeviceManager m_GraphicsDeviceManager;
        private GraphicsDevice m_GraphicsDevice;

        public AssetLoader(GraphicsDeviceManager graphicsDeviceManager)
        {
            m_GraphicsDeviceManager = graphicsDeviceManager;
        }

        public void Initialize()
        {
            m_GraphicsDevice = m_GraphicsDeviceManager.GraphicsDevice;
        }

        public Texture2D LoadTextureFromFile(string filePath)
        {
            using var fileStream = new FileStream(GetPath(filePath), FileMode.Open);
            return Texture2D.FromStream(m_GraphicsDevice, fileStream);
        }

        private string GetPath(string path)
        {
            string projectDirectory = Environment.CurrentDirectory.Split("bin")[0];

            string assetDirectory = Path.Join(projectDirectory, DIRECTORY);
            if (!Directory.Exists(assetDirectory))
                throw new Exception("Could not find the assets directory");

            string normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
            return Path.Join(assetDirectory, normalizedPath);
        }
    }
}