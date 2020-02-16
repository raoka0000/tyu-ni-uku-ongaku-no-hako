#import <UIKit/UIKit.h>
//#import <MediaPlayer/MediaPlayer.h>
#import "ViewController.h"


extern UIViewController* UnityGetGLViewController();
ViewController *vwcView;

void Initialize_(){
    NSLog(@"起動1");
    vwcView = [[ViewController alloc] init];
    [vwcView initImageView];
    NSLog(@"end終わり終了fin");

    //[HogeClass hogeMethod];
    //UIViewController* parent = UnityGetGLViewController();
    //UIView *uv = [[UIView alloc] init];
    //uv.frame = CGRectMake(0, 0, 100, 100);
    //uv.backgroundColor = [UIColor blueColor];
    //[parent.view addSubview:uv];
}
