using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PlanetaryDiversity.Components
{
    /// <summary>
    /// Generic utility functions
    /// This file is a stripped version of Kopernicus Utility.cs, which is licensed under LGPL.
    /// </summary>
    public static class Utility
    {        
        /**
         * Recursively searches for a named PSystemBody
         *
         * @param body Parent body to begin search in
         * @param name Name of body to find
         * 
         * @return Desired body or null if not found
         */
        public static PSystemBody FindBody(PSystemBody body, String name)
        {
            // Is this the body wer are looking for?
            if (body.celestialBody.bodyName == name)
                return body;

            // Otherwise search children
            foreach (PSystemBody child in body.children)
            {
                PSystemBody b = FindBody(child, name);
                if (b != null)
                    return b;
            }

            // Return null because we didn't find shit
            return null;
        }

        public static void UpdateScaledMesh(GameObject scaledVersion, PQS pqs, CelestialBody body, String path)
        {
            const Double rJool = 6000000.0;
            const Single rScaled = 1000.0f;

            // Compute scale between Jool and this body
            Single scale = (Single)(body.Radius / rJool);
            scaledVersion.transform.localScale = new Vector3(scale, scale, scale);

            Mesh scaledMesh;
            // Attempt to load a cached version of the scale space
            String CacheDirectory = KSPUtil.ApplicationRootPath + path;
            String CacheFile = CacheDirectory + "/" + body.name + ".bin";
            Directory.CreateDirectory(CacheDirectory);

            if (File.Exists(CacheFile))
            {
                scaledMesh = DeserializeMesh(CacheFile);
                RecalculateTangents(scaledMesh);
                scaledVersion.GetComponent<MeshFilter>().sharedMesh = scaledMesh;
            }

            // Otherwise we have to generate the mesh
            else
            {
                scaledMesh = ComputeScaledSpaceMesh(body, pqs);
                RecalculateTangents(scaledMesh);
                scaledVersion.GetComponent<MeshFilter>().sharedMesh = scaledMesh;
                SerializeMesh(scaledMesh, CacheFile);
            }

            // Apply mesh to the body
            SphereCollider collider = scaledVersion.GetComponent<SphereCollider>();
            if (collider != null) collider.radius = rScaled;
            if (pqs != null && scaledVersion.gameObject != null && scaledVersion.gameObject.transform != null)
            {
                scaledVersion.gameObject.transform.localScale = Vector3.one * (Single)(pqs.radius / rJool);
            }
        }


        // Generate the scaled space mesh using PQS (all results use scale of 1)
        public static Mesh ComputeScaledSpaceMesh(CelestialBody body, PQS pqsVersion)
        {
            // We need to get the body for Jool (to steal it's mesh)
            const Double rScaledJool = 1000.0f;
            Double rMetersToScaledUnits = (Single)(rScaledJool / body.Radius);

            // Generate a duplicate of the Jool mesh
            Mesh mesh = DuplicateMesh(Templates.ReferenceGeosphere);

            // If this body has a PQS, we can create a more detailed object
            if (pqsVersion != null)
            {
                // Find the PQS mods and enable the PQS-sphere
                IEnumerable<PQSMod> mods = pqsVersion.GetComponentsInChildren<PQSMod>(true).Where(m => m.modEnabled).OrderBy(m => m.order);
                foreach (PQSMod flatten in mods.Where(m => m is PQSMod_FlattenArea))
                    flatten.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(f => f.FieldType == typeof(Boolean)).SetValue(flatten, true);
                
                pqsVersion.isBuildingMaps = true;

                // If we were able to find PQS mods
                if (mods.Any())
                {
                    // Generate the PQS modifications
                    Vector3[] vertices = mesh.vertices;
                    for (Int32 i = 0; i < mesh.vertexCount; i++)
                    {
                        // Get the UV coordinate of this vertex
                        Vector2 uv = mesh.uv[i];

                        // Since this is a geosphere, normalizing the vertex gives the direction from center center
                        Vector3 direction = vertices[i];
                        direction.Normalize();

                        // Build the vertex data object for the PQS mods
                        PQS.VertexBuildData vertex = new PQS.VertexBuildData();
                        vertex.directionFromCenter = direction;
                        vertex.vertHeight = body.Radius;
                        vertex.u = uv.x;
                        vertex.v = uv.y;

                        // Build from the PQS
                        foreach (PQSMod mod in mods)
                            mod.OnVertexBuildHeight(vertex);

                        // Check for sea level
                        if (body.ocean && vertex.vertHeight < body.Radius)
                            vertex.vertHeight = body.Radius;

                        // Adjust the displacement
                        vertices[i] = direction * (Single)(vertex.vertHeight * rMetersToScaledUnits);
                    }
                    mesh.vertices = vertices;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                }

                // Cleanup
                pqsVersion.isBuildingMaps = false;
            }

            // Return the generated scaled space mesh
            return mesh;
        }

        public static Mesh DuplicateMesh(Mesh source)
        {
            // Create new mesh object
            Mesh dest = new Mesh();
            if (source == null)
            {
                Debug.Log("DuplicateMesh, source is null");
                return dest;
            }
            //ProfileTimer.Push("CopyMesh");
            Vector3[] verts = new Vector3[source.vertexCount];
            source.vertices.CopyTo(verts, 0);
            dest.vertices = verts;

            Int32[] tris = new Int32[source.triangles.Length];
            source.triangles.CopyTo(tris, 0);
            dest.triangles = tris;

            Vector2[] uvs = new Vector2[source.uv.Length];
            source.uv.CopyTo(uvs, 0);
            dest.uv = uvs;

            Vector2[] uv2s = new Vector2[source.uv2.Length];
            source.uv2.CopyTo(uv2s, 0);
            dest.uv2 = uv2s;

            Vector3[] normals = new Vector3[source.normals.Length];
            source.normals.CopyTo(normals, 0);
            dest.normals = normals;

            Vector4[] tangents = new Vector4[source.tangents.Length];
            source.tangents.CopyTo(tangents, 0);
            dest.tangents = tangents;

            Color[] colors = new Color[source.colors.Length];
            source.colors.CopyTo(colors, 0);
            dest.colors = colors;

            Color32[] colors32 = new Color32[source.colors32.Length];
            source.colors32.CopyTo(colors32, 0);
            dest.colors32 = colors32;

            //ProfileTimer.Pop("CopyMesh");
            return dest;
        }

        public static void RecalculateTangents(Mesh theMesh)
        {
            Int32 vertexCount = theMesh.vertexCount;
            Vector3[] vertices = theMesh.vertices;
            Vector3[] normals = theMesh.normals;
            Vector2[] texcoords = theMesh.uv;
            Int32[] triangles = theMesh.triangles;
            Int32 triangleCount = triangles.Length / 3;

            Vector4[] tangents = new Vector4[vertexCount];
            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Int32 tri = 0;

            for (Int32 i = 0; i < (triangleCount); i++)
            {
                Int32 i1 = triangles[tri];
                Int32 i2 = triangles[tri + 1];
                Int32 i3 = triangles[tri + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = texcoords[i1];
                Vector2 w2 = texcoords[i2];
                Vector2 w3 = texcoords[i3];

                Single x1 = v2.x - v1.x;
                Single x2 = v3.x - v1.x;
                Single y1 = v2.y - v1.y;
                Single y2 = v3.y - v1.y;
                Single z1 = v2.z - v1.z;
                Single z2 = v3.z - v1.z;

                Single s1 = w2.x - w1.x;
                Single s2 = w3.x - w1.x;
                Single t1 = w2.y - w1.y;
                Single t2 = w3.y - w1.y;

                Single r = 1.0f / (s1 * t2 - s2 * t1);
                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;
            }
            for (Int32 i = 0; i < (vertexCount); i++)
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[i].x = t.x;
                tangents[i].y = t.y;
                tangents[i].z = t.z;

                // Calculate handedness
                tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
            }
            theMesh.tangents = tangents;
        }

        // Serialize a mesh to disk
        public static void SerializeMesh(Mesh mesh, String path)
        {
            // Open an output filestream
            FileStream outputStream = new FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(outputStream);

            // Write the vertex count of the mesh
            writer.Write(mesh.vertices.Length);
            foreach (Vector3 vertex in mesh.vertices)
            {
                writer.Write(vertex.x);
                writer.Write(vertex.y);
                writer.Write(vertex.z);
            }
            writer.Write(mesh.uv.Length);
            foreach (Vector2 uv in mesh.uv)
            {
                writer.Write(uv.x);
                writer.Write(uv.y);
            }
            writer.Write(mesh.triangles.Length);
            foreach (Int32 triangle in mesh.triangles)
            {
                writer.Write(triangle);
            }

            // Finish writing
            writer.Close();
            outputStream.Close();
        }

        // Deserialize a mesh from disk
        public static Mesh DeserializeMesh(String path)
        {
            FileStream inputStream = new FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            BinaryReader reader = new BinaryReader(inputStream);

            // Get the vertices
            Int32 count = reader.ReadInt32();
            Vector3[] vertices = new Vector3[count];
            for (Int32 i = 0; i < count; i++)
            {
                Vector3 vertex;
                vertex.x = reader.ReadSingle();
                vertex.y = reader.ReadSingle();
                vertex.z = reader.ReadSingle();
                vertices[i] = vertex;
            }

            // Get the uvs
            Int32 uv_count = reader.ReadInt32();
            Vector2[] uvs = new Vector2[uv_count];
            for (Int32 i = 0; i < uv_count; i++)
            {
                Vector2 uv;
                uv.x = reader.ReadSingle();
                uv.y = reader.ReadSingle();
                uvs[i] = uv;
            }

            // Get the triangles
            Int32 tris_count = reader.ReadInt32();
            Int32[] triangles = new Int32[tris_count];
            for (Int32 i = 0; i < tris_count; i++)
                triangles[i] = reader.ReadInt32();

            // Close
            reader.Close();
            inputStream.Close();

            // Create the mesh
            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = triangles;
            m.uv = uvs;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // Credit goes to Kragrathea.
        public static Texture2D BumpToNormalMap(Texture2D source, Single strength)
        {
            strength = Mathf.Clamp(strength, 0.0F, 10.0F);
            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
            for (Int32 by = 0; by < result.height; by++)
            {
                for (Int32 bx = 0; bx < result.width; bx++)
                {
                    Single xLeft = source.GetPixel(bx - 1, by).grayscale * strength;
                    Single xRight = source.GetPixel(bx + 1, by).grayscale * strength;
                    Single yUp = source.GetPixel(bx, by - 1).grayscale * strength;
                    Single yDown = source.GetPixel(bx, by + 1).grayscale * strength;
                    Single xDelta = ((xLeft - xRight) + 1) * 0.5f;
                    Single yDelta = ((yUp - yDown) + 1) * 0.5f;
                    result.SetPixel(bx, by, new Color(yDelta, yDelta, yDelta, xDelta));
                }
            }
            result.Apply();
            return result;
        }
        
        // Whole file buffer management
        private static Byte[] wholeFileBuffer = null;
        private static Int32 sizeWholeFile = 0;
        private static Int32 arrayLengthOffset = 0;

        private static Byte[] LoadWholeFile(String path)
        {
            // If we haven't worked out if we can patch array length then do it
            if (arrayLengthOffset == 0)
                CalculateArrayLengthOffset();

            // If we can't patch array length then just use the normal function
            if (arrayLengthOffset == 1)
                return File.ReadAllBytes(path);

            // Otherwise we do cunning stuff
            FileStream file = File.OpenRead(path);
            if (file.Length > Int32.MaxValue)
                throw new Exception("File too large");

            Int32 fileBytes = (Int32)file.Length;

            if (wholeFileBuffer == null || fileBytes > sizeWholeFile)
            {
                // Round it up to a 1MB multiple
                sizeWholeFile = (fileBytes + 0xFFFFF) & ~0xFFFFF;
                Debug.Log("[PlaneraryDiversity] LoadWholeFile reallocating buffer to " + sizeWholeFile);
                wholeFileBuffer = new Byte[sizeWholeFile];
            }
            else
            {
                // Reset the length of the array to the full size
                FudgeByteArrayLength(wholeFileBuffer, sizeWholeFile);
            }

            // Read all the data from the file
            Int32 i = 0;
            while (fileBytes > 0)
            {
                Int32 read = file.Read(wholeFileBuffer, i, (fileBytes > 0x100000) ? 0x100000 : fileBytes);
                if (read <= 0) continue;
                i += read;
                fileBytes -= read;
            }

            // Fudge the length of the array
            FudgeByteArrayLength(wholeFileBuffer, i);

            return wholeFileBuffer;
        }

        private static Byte[] LoadRestOfReader(BinaryReader reader)
        {
            // If we haven't worked out if we can patch array length then do it
            if (arrayLengthOffset == 0)
                CalculateArrayLengthOffset();

            Int64 chunkBytes = reader.BaseStream.Length - reader.BaseStream.Position;
            if (chunkBytes > Int32.MaxValue)
                throw new Exception("Chunk too large");

            // If we can't patch array length then just use the normal function
            if (arrayLengthOffset == 1)
                return reader.ReadBytes((Int32)chunkBytes);

            // Otherwise we do cunning stuff
            Int32 fileBytes = (Int32)chunkBytes;
            if (wholeFileBuffer == null || fileBytes > sizeWholeFile)
            {
                // Round it up to a 1MB multiple
                sizeWholeFile = (fileBytes + 0xFFFFF) & ~0xFFFFF;
                Debug.Log("[PlaneraryDiversity] LoadRestOfReader reallocating buffer to " + sizeWholeFile);
                wholeFileBuffer = new Byte[sizeWholeFile];
            }
            else
            {
                // Reset the length of the array to the full size
                FudgeByteArrayLength(wholeFileBuffer, sizeWholeFile);
            }

            // Read all the data from the file
            Int32 i = 0;
            while (fileBytes > 0)
            {
                Int32 read = reader.Read(wholeFileBuffer, i, (fileBytes > 0x100000) ? 0x100000 : fileBytes);
                if (read > 0)
                {
                    i += read;
                    fileBytes -= read;
                }
            }

            // Fudge the length of the array
            FudgeByteArrayLength(wholeFileBuffer, i);

            return wholeFileBuffer;
        }

        private static unsafe void CalculateArrayLengthOffset()
        {
            // Work out the offset by allocating a small array and searching backwards until we find the correct value
            Int32[] temp = new Int32[3];
            Int32 offset = -4;
            fixed (Int32* ptr = &temp[0])
            {
                Int32* p = ptr - 1;
                while (*p != 3 && offset > -44)
                {
                    offset -= 4;
                    p--;
                }

                arrayLengthOffset = (*p == 3) ? offset : 1;
                Debug.Log("[PlaneraryDiversity] CalculateArrayLengthOffset using offset of " + arrayLengthOffset);
            }
        }

        private static unsafe void FudgeByteArrayLength(Byte[] array, Int32 len)
        {
            fixed (Byte* ptr = &array[0])
            {
                Int32* pLen = (Int32*)(ptr + arrayLengthOffset);
                *pLen = len;
            }
        }

        // Loads a texture
        public static Texture2D LoadTexture(String path, Boolean compress, Boolean upload, Boolean unreadable)
        {
            Texture2D map = null;
            if (File.Exists(path))
            {
                Boolean uncaught = true;
                try
                {
                    if (path.ToLower().EndsWith(".dds"))
                    {
                        // Borrowed from stock KSP 1.0 DDS loader (hi Mike!)
                        // Also borrowed the extra bits from Sarbian.
                        BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
                        UInt32 num = binaryReader.ReadUInt32();
                        if (num == DDSHeaders.DDSValues.uintMagic)
                        {

                            DDSHeaders.DDSHeader dDSHeader = new DDSHeaders.DDSHeader(binaryReader);

                            if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDX10)
                            {
                                new DDSHeaders.DDSHeaderDX10(binaryReader);
                            }

                            Boolean alpha = (dDSHeader.dwFlags & 0x00000002) != 0;
                            Boolean fourcc = (dDSHeader.dwFlags & 0x00000004) != 0;
                            Boolean rgb = (dDSHeader.dwFlags & 0x00000040) != 0;
                            Boolean alphapixel = (dDSHeader.dwFlags & 0x00000001) != 0;
                            Boolean luminance = (dDSHeader.dwFlags & 0x00020000) != 0;
                            Boolean rgb888 = dDSHeader.ddspf.dwRBitMask == 0x000000ff && dDSHeader.ddspf.dwGBitMask == 0x0000ff00 && dDSHeader.ddspf.dwBBitMask == 0x00ff0000;
                            //bool bgr888 = dDSHeader.ddspf.dwRBitMask == 0x00ff0000 && dDSHeader.ddspf.dwGBitMask == 0x0000ff00 && dDSHeader.ddspf.dwBBitMask == 0x000000ff;
                            Boolean rgb565 = dDSHeader.ddspf.dwRBitMask == 0x0000F800 && dDSHeader.ddspf.dwGBitMask == 0x000007E0 && dDSHeader.ddspf.dwBBitMask == 0x0000001F;
                            Boolean argb4444 = dDSHeader.ddspf.dwABitMask == 0x0000f000 && dDSHeader.ddspf.dwRBitMask == 0x00000f00 && dDSHeader.ddspf.dwGBitMask == 0x000000f0 && dDSHeader.ddspf.dwBBitMask == 0x0000000f;
                            Boolean rbga4444 = dDSHeader.ddspf.dwABitMask == 0x0000000f && dDSHeader.ddspf.dwRBitMask == 0x0000f000 && dDSHeader.ddspf.dwGBitMask == 0x000000f0 && dDSHeader.ddspf.dwBBitMask == 0x00000f00;

                            Boolean mipmap = (dDSHeader.dwCaps & DDSHeaders.DDSPixelFormatCaps.MIPMAP) != (DDSHeaders.DDSPixelFormatCaps)0u;
                            Boolean isNormalMap = ((dDSHeader.ddspf.dwFlags & 524288u) != 0u || (dDSHeader.ddspf.dwFlags & 2147483648u) != 0u);
                            if (fourcc)
                            {
                                if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT1)
                                {
                                    map = new Texture2D((Int32)dDSHeader.dwWidth, (Int32)dDSHeader.dwHeight, TextureFormat.DXT1, mipmap);
                                    map.LoadRawTextureData(LoadRestOfReader(binaryReader));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT3)
                                {
                                    map = new Texture2D((Int32)dDSHeader.dwWidth, (Int32)dDSHeader.dwHeight, (TextureFormat)11, mipmap);
                                    map.LoadRawTextureData(LoadRestOfReader(binaryReader));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT5)
                                {
                                    map = new Texture2D((Int32)dDSHeader.dwWidth, (Int32)dDSHeader.dwHeight, TextureFormat.DXT5, mipmap);
                                    map.LoadRawTextureData(LoadRestOfReader(binaryReader));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT2)
                                {
                                    Debug.Log("[PlaneraryDiversity]: DXT2 not supported" + path);
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT4)
                                {
                                    Debug.Log("[PlaneraryDiversity]: DXT4 not supported: " + path);
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDX10)
                                {
                                    Debug.Log("[PlaneraryDiversity]: DX10 dds not supported: " + path);
                                }
                                else
                                    fourcc = false;
                            }
                            if (!fourcc)
                            {
                                TextureFormat textureFormat = TextureFormat.ARGB32;
                                Boolean ok = true;
                                if (rgb && (rgb888 /*|| bgr888*/))
                                {
                                    // RGB or RGBA format
                                    textureFormat = alphapixel
                                    ? TextureFormat.RGBA32
                                    : TextureFormat.RGB24;
                                }
                                else if (rgb && rgb565)
                                {
                                    // Nvidia texconv B5G6R5_UNORM
                                    textureFormat = TextureFormat.RGB565;
                                }
                                else if (rgb && alphapixel && argb4444)
                                {
                                    // Nvidia texconv B4G4R4A4_UNORM
                                    textureFormat = TextureFormat.ARGB4444;
                                }
                                else if (rgb && alphapixel && rbga4444)
                                {
                                    textureFormat = TextureFormat.RGBA4444;
                                }
                                else if (!rgb && alpha != luminance)
                                {
                                    // A8 format or Luminance 8
                                    textureFormat = TextureFormat.Alpha8;
                                }
                                else
                                {
                                    ok = false;
                                    Debug.Log("[PlaneraryDiversity]: Only DXT1, DXT5, A8, RGB24, RGBA32, RGB565, ARGB4444 and RGBA4444 are supported");
                                }
                                if (ok)
                                {
                                    map = new Texture2D((Int32)dDSHeader.dwWidth, (Int32)dDSHeader.dwHeight, textureFormat, mipmap);
                                    map.LoadRawTextureData(LoadRestOfReader(binaryReader));
                                }

                            }
                            if (map != null)
                                if (upload)
                                    map.Apply(false, unreadable);
                        }
                        else
                            Debug.Log("[PlaneraryDiversity]: Bad DDS header.");
                    }
                    else
                    {
                        map = new Texture2D(2, 2);
                        Byte[] data = LoadWholeFile(path);
                        if (data == null)
                            throw new Exception("LoadWholeFile failed");

                        map.LoadImage(data);
                        if (compress)
                            map.Compress(true);
                        if (upload)
                            map.Apply(false, unreadable);
                    }
                }
                catch (Exception ex)
                {
                    uncaught = false;
                    Debug.Log("[PlaneraryDiversity]: failed to load " + path + " with exception " + ex.Message);
                }
                if (map == null && uncaught)
                {
                    Debug.Log("[PlaneraryDiversity]: failed to load " + path);
                }
                map.name = path;
            }
            else
                Debug.Log("[PlaneraryDiversity]: texture does not exist! " + path);

            return map;
        }

        /// <summary>
        /// Converts an unreadable texture into a readable one
        /// </summary>
        public static Texture2D CreateReadable(Texture2D original)
        {
            // Checks
            if (original == null) return null;
            if (original.width == 0 || original.height == 0) return null;

            // Create the new texture
            Texture2D finalTexture = new Texture2D(original.width, original.height);

            // isn't read or writeable ... we'll have to get tricksy
            RenderTexture rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);
            Graphics.Blit(original, rt);
            RenderTexture.active = rt;

            // Load new texture
            finalTexture.ReadPixels(new Rect(0, 0, finalTexture.width, finalTexture.height), 0, 0);

            // Kill the old one
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            // Return
            return finalTexture;
        }

        /// <summary>
        /// Extracts the average color from a texture file
        /// </summary>
        public static Color GetAverageColor(Texture2D texture)
        {
            Byte avgB;
            Byte avgG;
            Byte avgR;
            Int64[] totals = { 0, 0, 0 };

            Int64 width = texture.width;
            Int64 height = texture.height;

            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Color32 c = texture.GetPixel(x, y);
                    totals[0] += c.b;
                    totals[1] += c.g;
                    totals[2] += c.r;
                }
            }

            avgB = (Byte)(totals[0] / (width * height));
            avgG = (Byte)(totals[1] / (width * height));
            avgR = (Byte)(totals[2] / (width * height));
            return new Color32(avgR, avgG, avgB, 255);
        }
        
        /// <summary>
        /// Modifies a color so that it becomes a multiplier color
        /// </summary>
        public static Color ReColor(Color c, Color average)
        {
            // Do some maths..
            return new Color(c.r / average.r, c.g / average.g, c.b / average.b, 1);
        }

        /// <summary>
        /// Makes a color darker
        /// </summary>
        public static Color Dark(Color c)
        {
            if ((c.r > 0.5) || (c.g > 0.5) || (c.b > 0.5))
            {
                c = c * new Color(0.5f, 0.5f, 0.5f);
            }
            return c;
        }

        /// <summary>
        /// An array of all XKCD colors
        /// </summary>
        private static Color[] _colors;

        public static Color[] colors
        {
            get
            {
                if (_colors == null)
                    _colors = typeof(XKCDColors).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Color)).Select(p => (Color)p.GetValue(null, null)).ToArray();
                return _colors;
            }
        }

        // Runs a function recursively
        public static TOut DoRecursive<TIn, TOut>(TIn start, Func<TIn, IEnumerable<TIn>> selector, Func<TOut, Boolean> check, Func<TIn, TOut> action)
        {
            TOut tout = action(start);
            if (check(tout))
                return tout;
            foreach (TIn tin in selector(start))
            {
                tout = DoRecursive(tin, selector, check, action);
                if (check(tout))
                    return tout;
            }
            return default(TOut);
        }

        // Runs a function recursively
        public static void DoRecursive<T>(T start, Func<T, IEnumerable<T>> selector, Action<T> action)
        {
            DoRecursive<T, System.Object>(start, selector, tout => false, tin => { action(tin); return null; });
        }

        public static List<CelestialBody> GetSortedBodies()
        {
            List<CelestialBody> bodies = new List<CelestialBody>() { PSystemManager.Instance.localBodies[0] };
            DoRecursive(PSystemManager.Instance.localBodies[0], b => b.orbitingBodies != null && b.orbitingBodies.Any() ? 
                        b.orbitingBodies.OrderBy(b_ => b_.orbit?.semiMajorAxis).ToList() : new List<CelestialBody>(), b => bodies.Add(b));
            return bodies;
        }
    }
}
