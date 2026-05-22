# Turning satelite data into usable data 
In order to use the data from a Digital Elevation Model you need to turn the image into a 2D integer array. Than after transforming GPS coordinates of the source and reciever into indexes in the array you can use a [Line drawing algorithm](https://wikipedia.org/wiki/Line_drawing_algorithm) to find indexes of variables between them and turn them into a 1D array. From this moment there are two different ways to explain what happens - the math way and the code way.
# The math way - graph
This works by using the equation


\theta_{min} = \arctan\left(\max_{k=1}^{9}(L_{k+1} - L_k)\right)

Where L is the array , to calculate θ which is the minimal angle needed to keep LOS.
To make this equation easier to understand i made a [Demo in Desmos](https://desmos.com/calculator/4zc3jdxvck).
After that you just calculate the distance between source and reciever using the pythagorean theorem and that enables you to calculate the angle theta which  you can calculate with basic trigonometry from heigh and distance.
$$ θ = atan2(y,x)$$
Finally , you only have LOS if
$$ θ > θ_{min}$$

# The code way
_DISCLAIMER:THE CODE PRESENTED HERE IS SIMPLIFIED FOR THE SAKE OF READABILTY_
Coding it is actually
way easier since it's a singular for loop
```c#
public static double minTheta(int[] arr)
{
  double cur 0;
  for(int i = 1;i<arr.Length;i++)
 {
  if(cur<Math.Atan2(arr[i],i) * 180.0 / Math.PI)
  {
    cur = Math.Atan2(arr[i],i) * 180.0 / Math.PI;
  }
 }
  return cur;
}


```
this gives the min theta needed for confirming LOS and the rest is pretty much the same as in the math way.