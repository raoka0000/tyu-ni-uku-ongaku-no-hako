//
//  ViewController.m
//  Unity-iPhone
//
//  Created by raoka0000 on 2016/08/29.
//
//

#import <Foundation/Foundation.h>
#import "ViewController.h"

@interface ViewController ()
@property MPMusicPlayerController* player;
@property UIViewController *vwcUnityView;
@end


@implementation ViewController

- (void)viewDidLoad
{
    [super viewDidLoad];
    // Do any additional setup after loading the view, typically from a nib.

    self.player = [MPMusicPlayerController applicationMusicPlayer];
}

- (void) initImageView {
    // MPMediaPickerControllerのインスタンスを作成
    MPMediaPickerController *picker = [[MPMediaPickerController alloc]init];
    // ピッカーのデリゲートを設定
    picker.delegate = self;
    _vwcUnityView = UnityGetGLViewController();
    // 複数選択を不可にする。（YESにすると、複数選択できる）
    picker.allowsPickingMultipleItems = NO;
    // ピッカーを表示する
    [UnityGetGLViewController() presentViewController:picker animated:YES completion:nil];
}

// メディアアイテムピッカーでアイテムを選択完了したときに呼び出される
- (void)mediaPicker:(MPMediaPickerController *)mediaPicker didPickMediaItems:(MPMediaItemCollection *)mediaItemCollection
{
    // 選択した曲情報がmediaItemCollectionに入っているので、これをplayerにセット。

    //[self.player setQueueWithItemCollection:mediaItemCollection];
    // 再生開始
    //[self.player play];

    BOOL bol = [self exportItem:mediaItemCollection.representativeItem];
    if(bol){
        NSLog(@"成功した成功した成功した成功した成功した成功した成功した成功した成功した成功した成功した成功した");
        //NSLog(@"たどりついたか");
    }else{
        NSLog(@"失敗した失敗した失敗した失敗した失敗した失敗した失敗した失敗した失敗した失敗した失敗した失敗した");
    }


    // ピッカーを閉じ、破棄する
    [mediaPicker dismissViewControllerAnimated:YES completion:nil];
}

//選択がキャンセルされた場合に呼ばれる
- (void)mediaPickerDidCancel:(MPMediaPickerController *)mediaPicker{
    // ピッカーを閉じ、破棄する
    [mediaPicker dismissViewControllerAnimated:YES completion:nil];
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}


- (BOOL)exportItem:(MPMediaItem *)item
{
    NSError *error = nil;

    NSDictionary *audioSetting = [NSDictionary dictionaryWithObjectsAndKeys:
                                  [NSNumber numberWithFloat:44100.0],AVSampleRateKey,
                                  [NSNumber numberWithInt:2],AVNumberOfChannelsKey,
                                  [NSNumber numberWithInt:16],AVLinearPCMBitDepthKey,
                                  [NSNumber numberWithInt:kAudioFormatLinearPCM], AVFormatIDKey,
                                  [NSNumber numberWithBool:NO], AVLinearPCMIsFloatKey,
                                  [NSNumber numberWithBool:0], AVLinearPCMIsBigEndianKey,
                                  [NSNumber numberWithBool:NO], AVLinearPCMIsNonInterleaved,
                                  [NSData data], AVChannelLayoutKey, nil];

    //読み込み側のセットアップ

    NSURL *url = [item valueForProperty:MPMediaItemPropertyAssetURL];
    AVURLAsset *URLAsset = [AVURLAsset URLAssetWithURL:url options:nil];
    if (!URLAsset) return NO;

    AVAssetReader *assetReader = [AVAssetReader assetReaderWithAsset:URLAsset error:&error];
    if (error) return NO;

    NSArray *tracks = [URLAsset tracksWithMediaType:AVMediaTypeAudio];
    if (![tracks count]) return NO;

    AVAssetReaderAudioMixOutput *audioMixOutput = [AVAssetReaderAudioMixOutput
                                                   assetReaderAudioMixOutputWithAudioTracks:tracks
                                                   audioSettings:audioSetting];

    if (![assetReader canAddOutput:audioMixOutput]) return NO;

    [assetReader addOutput:audioMixOutput];

    if (![assetReader startReading]) return NO;


    //書き込み側のセットアップ

    //NSString *title = [item valueForProperty:MPMediaItemPropertyTitle];
    NSString *title =  @"extraAudio";//ファイル名変更
    NSArray *docDirs = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString *docDir = [docDirs objectAtIndex:0];
    NSString *outPath = [[docDir stringByAppendingPathComponent:title]
                         stringByAppendingPathExtension:@"wav"];

    NSURL *outURL = [NSURL fileURLWithPath:outPath];
    AVAssetWriter *assetWriter = [AVAssetWriter assetWriterWithURL:outURL
                                                          fileType:AVFileTypeWAVE
                                                             error:&error];
    if (error) return NO;

    NSFileManager *fileManager = [NSFileManager defaultManager];
    if ([fileManager fileExistsAtPath:outPath])
    {
        [fileManager removeItemAtURL:outURL error:nil];
    }

    AVAssetWriterInput *assetWriterInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeAudio
                                                                              outputSettings:audioSetting];
    assetWriterInput.expectsMediaDataInRealTime = NO;

    if (![assetWriter canAddInput:assetWriterInput]) return NO;

    [assetWriter addInput:assetWriterInput];

    if (![assetWriter startWriting]) return NO;



    //コピー処理

    //[assetReader retain];
    //[assetWriter retain];

    [assetWriter startSessionAtSourceTime:kCMTimeZero];

    dispatch_queue_t queue = dispatch_queue_create("assetWriterQueue", NULL);

    [assetWriterInput requestMediaDataWhenReadyOnQueue:queue usingBlock:^{

        NSLog(@"start");

        while (1)
        {
            if ([assetWriterInput isReadyForMoreMediaData]) {

                CMSampleBufferRef sampleBuffer = [audioMixOutput copyNextSampleBuffer];

                if (sampleBuffer) {
                    [assetWriterInput appendSampleBuffer:sampleBuffer];
                    CFRelease(sampleBuffer);
                } else {
                    [assetWriterInput markAsFinished];
                    break;
                }
            }
        }

        [assetWriter finishWriting];
        //[assetReader release];
        //[assetWriter release];

        NSLog(@"finish");
        UnitySendMessage("UIController", "SetAudioSource","");
    }];

    /*NSString *newNamePath = [[docDir stringByAppendingPathComponent:@"extraAudio"]
                         stringByAppendingPathExtension:@"wav"];
    NSURL *newNameURL = [NSURL fileURLWithPath:outPath];
    if ([fileManager fileExistsAtPath:outPath])
    {
        [fileManager moveItemAtPath:outURL toPath:newNameURL error:nil];
    }*/
    //dispatch_release(queue);

    return YES;
}

@end
