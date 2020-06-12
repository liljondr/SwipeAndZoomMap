using System;
using System.Collections;
using System.Collections.Generic;
using HedgehogTeam.EasyTouch;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private SpriteRenderer SpriteBackground;

    /// <summary> Розміри спрайту </summary>
    private Rect fieldSize;

    /// <summary> Скільки пікселів буде в одному юніті, тобто метрі. По замовчуванню 100 </summary>
    private float spritePixelsPerUnit;

    /// <summary> розмір камери, при якому найменша сторона спрайта повністю поміщається в камеру. Найдовша виходить за межі камери </summary>
    private float maxXSizeForCamera;

    /// <summary> розмір камери, при якому найбільша сторона спрайта повністю поміщається в камеру. по обидві сторони меншої сторони смуги фону </summary>
    private float maxYSizeForCamera;

    private Vector2 startPosCamera;
    private float maxSizeCamera;
    private float minSizeCamera;

    private float coefForZoom = 0.1f;
    private float coefForMoveInZoom = 0.3f;

    /// <summary>  крайня координата по Х для камери </summary>
    private float xEdge;
    /// <summary>  крайня координата по Y для камери </summary>
    private float yEdge;

    private Vector3 rayLeftUpCornerScreen;
    private Vector3 rayRightDownCornerScreen;
    private float widthScreenInWold;
    private float hightScreenInWold;

    private Vector2 positionOnTouch2Finger;
    private Vector2 positionTargetCamera;

    void OnEnable()
    {
        EasyTouch.On_TouchStart2Fingers += On_TouchStart2Fingers;
        EasyTouch.On_PinchIn += On_PinchIn;
        EasyTouch.On_PinchOut += On_PinchOut;
        EasyTouch.On_PinchEnd += On_PinchEnd;
        EasyTouch.On_Swipe += OnSwipe;
    }


    void Start()
    {
        fieldSize = SpriteBackground.sprite.rect;
        spritePixelsPerUnit = SpriteBackground.sprite.pixelsPerUnit;
        rayLeftUpCornerScreen = new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z);
        rayRightDownCornerScreen = new Vector3(0, 0, Camera.main.transform.position.z);
        startPosCamera = Camera.main.transform.position;

        CalculateSizeCamera();
        Recalculations();
    }

    private void CalculateSizeCamera()
    {
        maxXSizeForCamera = (fieldSize.width * 0.5f) / (Camera.main.aspect * spritePixelsPerUnit);
        maxYSizeForCamera = (fieldSize.height * 0.5f) / spritePixelsPerUnit;
        maxSizeCamera = Mathf.Max(maxXSizeForCamera, maxYSizeForCamera);
        minSizeCamera = Mathf.Min(maxXSizeForCamera, maxYSizeForCamera);
        //Debug.Log("maxXSizeForCamera= " + maxXSizeForCamera + ":: maxYSizeForCamera =  " + maxYSizeForCamera);
    }

    

    void OnSwipe(Gesture gesture)
    {
        int touchCount = EasyTouch.GetTouchCount();
        if (touchCount == 1)
        {
            Vector2 newPosition = GetNewCameraPositionInSwipe(gesture);
            Camera.main.transform.position =
                new Vector3(newPosition.x, newPosition.y, Camera.main.transform.position.z);
            //   Debug.Log("Camera.position = " + Camera.main.transform.position);
        }
    }

    private Vector2 GetNewCameraPositionInSwipe(Gesture gesture)
    {
        Vector2 deltaSwipe = gesture.swipeVector;
        float deltaXinWold = deltaSwipe.x * widthScreenInWold / Screen.width;
        float deltaYinWold = deltaSwipe.y * hightScreenInWold / Screen.height;
       // Debug.Log("deltaSwipe =" + deltaSwipe + " deltaXinWold= " + deltaXinWold + "   deltaYinWold= " + deltaYinWold);
        float newX = Camera.main.transform.position.x - deltaXinWold;
        newX = (Math.Abs(newX - startPosCamera.x) < xEdge - startPosCamera.x)
            ? newX
            : Camera.main.transform.position.x;
        float newY = Camera.main.transform.position.y - deltaYinWold;
        newY = (Math.Abs(newY - startPosCamera.y) < yEdge - startPosCamera.y)
            ? newY
            : Camera.main.transform.position.y;
        return new Vector2(newX, newY);
    }


    // At the 2 fingers touch beginning
    private void On_TouchStart2Fingers(Gesture gesture)
    {
        // Debug.Log("________________________________________On_TouchStart2Fingers");
        // disable twist gesture recognize for a real pinch end
        EasyTouch.SetEnableTwist(false);
        EasyTouch.SetEnablePinch(true);
        positionOnTouch2Finger = CalculateTouchPosition2Finger(gesture);
        // ForDebugLog(gesture);
    }

    // At the pinch in
    private void On_PinchIn(Gesture gesture)
    {
        float zoom = Time.deltaTime * gesture.deltaPinch;
        Camera.main.orthographicSize = CorectSizeCamera(Camera.main.orthographicSize + zoom * coefForZoom);
        //  Debug.Log("zoom = " + zoom + ":: MyCamera.orthographicSize = " + Camera.main.orthographicSize);
        Recalculations();
        MoveToPinchNew(zoom);
    }


    // At the pinch out
    private void On_PinchOut(Gesture gesture)
    {
      float zoom = Time.deltaTime * gesture.deltaPinch;
        // ForDebugLog(gesture);
        Camera.main.orthographicSize = CorectSizeCamera(Camera.main.orthographicSize - zoom * coefForZoom);
        //   Debug.Log("zoom = " + zoom + ":: CAMERASize = " + MyCamera.orthographicSize);
        Recalculations();
        MoveToPinchNew(zoom);
    }


    private void MoveToPinchNew(float zoom)
    {
        Vector3 position = Camera.main.transform.position;
        positionTargetCamera = positionOnTouch2Finger;
        float xCameraCurrent = XCameraCurrentNew(zoom, position.x);
        float yCameraCurrent = YCameraCurrentNew(zoom, position.y);
        Camera.main.transform.position = new Vector3(xCameraCurrent, yCameraCurrent, position.z);

        //  Debug.Log(" positionTargetCamera = "+ positionTargetCamera);
    }


    private float XCameraCurrentNew(float zoom, float positionX)
    {
        float deltaMove = Mathf.Sign(positionTargetCamera.x - positionX) * zoom * coefForMoveInZoom * maxSizeCamera /
                          Camera.main.orthographicSize;
        deltaMove = (float) Math.Round(deltaMove, 2);
        float xCameraCurrent = positionX + deltaMove;
        float controlFinishX = (float) Math.Round(positionTargetCamera.x - xCameraCurrent, 2);
        if (Mathf.Abs(controlFinishX) <= Mathf.Abs(deltaMove) && Mathf.Sign(controlFinishX) != Mathf.Sign(deltaMove))
        {
            xCameraCurrent = positionTargetCamera.x;
        }

        //якщо цільова координата зайшла за межі спрайту, вибираємо край
        xCameraCurrent = BorderCheck(xCameraCurrent, startPosCamera.x, xEdge);

        return xCameraCurrent;
    }


    private float YCameraCurrentNew(float zoom, float positionY)
    {
        float deltaMove = Mathf.Sign(positionTargetCamera.y - positionY) * zoom * coefForMoveInZoom * minSizeCamera / Camera.main.orthographicSize;
        deltaMove = (float)Math.Round(deltaMove, 2);
        float yCameraCurrent = positionY + deltaMove;
        float controlFinishY = (float)Math.Round(positionTargetCamera.y - yCameraCurrent, 2);
        if (Mathf.Abs(controlFinishY) <= Mathf.Abs(deltaMove) && Mathf.Sign(controlFinishY) != Mathf.Sign(deltaMove))
        {
            yCameraCurrent = positionTargetCamera.y;
        }

        yCameraCurrent = BorderCheck(yCameraCurrent, startPosCamera.y, yEdge);
        //   Debug.Log("positionTargetCamera.y= " + positionTargetCamera.y + "  ::deltaMove=" + deltaMove + " ::  yCameraCurrent =" + yCameraCurrent);
        return yCameraCurrent;
    }

    private float BorderCheck(float current, float start, float edge)
    {
        float deltaMax = Mathf.Abs(start - edge);
        float deltaCurrent = Mathf.Abs(start - current);
        //якщо поточне значення не заходить за границі допустипого вертаємо поточне значення
        if (Mathf.Sign(deltaMax - deltaCurrent) > 0)
        {
            return current;
        }
        //інакше вертаємо необхідне граничне значення
        else
        {
            if (Mathf.Sign((current - start)) > 0)
            {
                return edge;
            }
            else
            {
                float oppositeExtreme = 2 * start - edge;
                return oppositeExtreme;
            }
        }
    }

    // At the pinch end
    private void On_PinchEnd(Gesture gesture)
    {
        EasyTouch.SetEnableTwist(true);
    }

    /// <summary> Корекція розміру камери</summary>
    private float CorectSizeCamera(float size)
    {
        if (size > maxSizeCamera)
        {
            size = maxSizeCamera;
        }
        else if (size < 0.25f)
        {
            size = 0.25f;
        }

        return size;
    }

    private void Recalculations()
    {
        CalculateEdgePositions();
        CalculateSizeScreenInWoldCoord();
    }

    private void CalculateEdgePositions()
    {
        xEdge = Camera.main.aspect * (maxXSizeForCamera - Camera.main.orthographicSize) + startPosCamera.x;
        if (xEdge < startPosCamera.x)
        {
            xEdge = startPosCamera.x;
        }

        yEdge = maxYSizeForCamera - Camera.main.orthographicSize + startPosCamera.y;
        if (yEdge < startPosCamera.y)
        {
            yEdge = startPosCamera.y;
        }

       // Debug.Log("xExtreme = " + xEdge + ":: yExtreme = " + yEdge);
    }

    private void CalculateSizeScreenInWoldCoord()
    {
        Vector3 rayLeftUpInWold = Camera.main.ScreenToWorldPoint(rayLeftUpCornerScreen);
        Vector3 rayRightDownInWold = Camera.main.ScreenToWorldPoint(rayRightDownCornerScreen);
        widthScreenInWold = rayLeftUpInWold.x - rayRightDownInWold.x;
        hightScreenInWold = rayLeftUpInWold.y - rayRightDownInWold.y;

       // Debug.Log("rayLeftUpInWold =" + rayLeftUpInWold + " ::  rayRightDownInWold= " + rayRightDownInWold);
      //  Debug.Log("widthScreenInWold =" + widthScreenInWold + " ::  hightScreenInWold= " + hightScreenInWold);
    }

    private Vector2 CalculateTouchPosition2Finger(Gesture gesture)
    {
        float xTouch = Camera.main.transform.position.x + Camera.main.aspect * Camera.main.orthographicSize *
                       (gesture.startPosition.x / (Camera.main.pixelWidth * 0.5f) - 1);
        float yTouch = Camera.main.transform.position.y + Camera.main.orthographicSize *
                       (gesture.startPosition.y / (Camera.main.pixelHeight * 0.5f) - 1);
        Vector2 positionOnTouch = new Vector2(xTouch, yTouch);
        // Debug.Log("CalculateTouchPosition2Finger in (" + xTouch + "; " + yTouch + ")");

        return positionOnTouch;
    }

    private void OnDisable()
    {
        EasyTouch.On_TouchStart2Fingers -= On_TouchStart2Fingers;
        EasyTouch.On_PinchIn -= On_PinchIn;
        EasyTouch.On_PinchOut -= On_PinchOut;
        EasyTouch.On_PinchEnd -= On_PinchEnd;
        EasyTouch.On_Swipe -= OnSwipe;
       
    }
}