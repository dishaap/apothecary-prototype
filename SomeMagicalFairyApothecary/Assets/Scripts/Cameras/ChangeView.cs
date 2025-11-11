using UnityEngine;

public class ChangeView : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private int numberOfViews;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            SnapToView(0);

        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            SnapToView(1);
    }

    private void SnapToView(int direction)
    {
        
        switch(direction)
        {
            case 0:
                cameraTransform.rotation = Quaternion.Euler(transform.eulerAngles.x, (transform.eulerAngles.y + 360 / numberOfViews) % 360, transform.eulerAngles.z);
                break;
            case 1:
                cameraTransform.rotation = Quaternion.Euler(transform.eulerAngles.x, (transform.eulerAngles.y - 360 / numberOfViews) % 360, transform.eulerAngles.z);
                break;
        }
    }
}
