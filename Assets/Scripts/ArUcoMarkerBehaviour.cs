using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VisComp;
using OpenCvSharp;
using OpenCvSharp.Aruco;

public class ArUcoMarkerBehaviour : MonoBehaviour
{
    #region CONSTANTS

    public Dictionary ar_dict = CvAruco.GetPredefinedDictionary(OpenCvSharp.Aruco.PredefinedDictionaryName.Dict6X6_250);

    const int block_per_marker = 8; // Dict6x6 = 8x8 blocks = marker
    const int pixel_per_block  = 16; // [CAUTION] include white spaces between markers

    #endregion // CONSTANTS



    #region PUBLIC_MEMBERS

    public float marker_mm = 30.0f; // 30.0 [mm]
    public int   marker_width  = 8;
    public int   marker_height = 5;

    [HideInInspector]
    public Material material = null;
    public Dictionary<int, Point3f[]> markermap_corners; // < marker id, four corners (LT, RT, RB, LB) >

    [HideInInspector]
    public List<int> marker_ids;

    #endregion // PUBLIC_MEMBERS



    #region PUBLIC_METHODS

    public void CreateMarkerTexture2D()
    {
        if (null != material) RemoveMarkerTexture2D();

        // create markermap
        Mat img_bgr = CreateMarkerMap();
        //Cv2.ImShow("ArUcoMarkerMap", img_bgr); // [CHECK: OK]

        Texture2D tex2D = img_bgr.ToTexture2D();

        // mesh for markermap: [mm] to [m]
        float half_scale_x = 0.5f * W_mm * 0.001f;
        float half_scale_z = 0.5f * H_mm * 0.001f;

        // add Components to this GameObject
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = Helper.GetBoardMesh(half_scale_x, half_scale_z);

        material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = tex2D;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
    }

    public void ExportPDF(string filepath)
    {
        Mat img_bgr = material.mainTexture.ToMat();
        Helper.ExportPDF(filepath, img_bgr, new Vector2(W_mm, H_mm));
    }

    public void RemoveMarkerTexture2D()
    {
        DestroyImmediate(material);

        var mf = gameObject.GetComponent<MeshFilter>();
        if (null != mf) DestroyImmediate(mf);

        var mr = gameObject.GetComponent<MeshRenderer>();
        if (null != mr) DestroyImmediate(mr);
    }

    #endregion // PUBLIC_METHODS



    #region SUBROUTINES

    int   W, H;
    float W_mm, H_mm;

    private Point3f PixelToObject(int img_x, int img_y)
    {
        float obj_x = W_mm / (float)(W - 1) * img_x - 0.5f * W_mm;
        float obj_y = H_mm / (float)(H - 1) * img_y - 0.5f * H_mm;

        // [mm] to [m]
        obj_x *= 0.001f;
        obj_y *= 0.001f;

        return new Point3f(obj_x, obj_y, 0.0f);
    }

    Mat CreateMarkerMap()
    {
        // object (unit: [mm])
        float mm_per_block = marker_mm / block_per_marker;
        W_mm = mm_per_block * (marker_width  * (block_per_marker + 1) - 1);
        H_mm = mm_per_block * (marker_height * (block_per_marker + 1) - 1);

        markermap_corners = new Dictionary<int, Point3f[]>();
        
        // image (unit: pixel)
        H = pixel_per_block * (marker_height * (block_per_marker + 1) - 1);
        W = pixel_per_block * (marker_width  * (block_per_marker + 1) - 1);
        int pixel_per_marker = pixel_per_block * block_per_marker;

        // marker image
        Mat marker_bgr = new Mat(H, W, MatType.CV_8UC3, new Scalar(255, 255, 255));

        marker_ids = Enumerable.Range(0, 250).ToList(); // maximum: 250
        // [TODO: shuffle] data for randomization
        marker_ids = marker_ids.GetRange(0, marker_width * marker_height); // cut the list

        int marker_id = 0;
        for (int j = 0; j < marker_height; j++)
        for (int i = 0; i < marker_width ; i++)
        {
            Mat buf = new Mat();
            CvAruco.DrawMarker(ar_dict, marker_id, pixel_per_marker, buf);
            Cv2.CvtColor(buf, buf, ColorConversionCodes.GRAY2BGR);
                
            var rect = new OpenCvSharp.Rect(
                i * pixel_per_block * (block_per_marker + 1),
                j * pixel_per_block * (block_per_marker + 1),
                pixel_per_marker, pixel_per_marker);
                
            // four corners (LT, RT, RB, LB)
            markermap_corners.Add(marker_id, new Point3f[] {
                PixelToObject(rect.Left , rect.Top),
                PixelToObject(rect.Left + rect.Width, rect.Top),
                PixelToObject(rect.Left + rect.Width, rect.Top + rect.Height),
                PixelToObject(rect.Left , rect.Top + rect.Height),
            } );

            // append marker on the image
            var pos = new Mat(marker_bgr, rect);
            buf.CopyTo(pos);

            marker_id++;
        }

        return marker_bgr;
    }

    #endregion // SUBROUTINES
}
