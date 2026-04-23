using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private float scrollSpeedX, scrollSpeedY;


    // Update is called once per frame
    void Update()
    {
        backgroundImage.uvRect = new Rect(backgroundImage.uvRect.position + new Vector2(scrollSpeedX, scrollSpeedY) * Time.deltaTime, backgroundImage.uvRect.size);
    }
}
