#import <UIKit/UIKit.h>
#import "UnityInterface.h"

extern "C" void FoodieMatchShareText(
    const char *title,
    const char *message)
{
    NSString *shareTitle =
        title != nullptr
            ? [NSString stringWithUTF8String:title]
            : @"";
    NSString *shareMessage =
        message != nullptr
            ? [NSString stringWithUTF8String:message]
            : @"";

    dispatch_async(dispatch_get_main_queue(), ^{
        UIActivityViewController *controller =
            [[UIActivityViewController alloc]
                initWithActivityItems:@[shareMessage]
                applicationActivities:nil];
        controller.title = shareTitle;

        UIViewController *rootController =
            UnityGetGLViewController();

        if (controller.popoverPresentationController != nil)
        {
            controller.popoverPresentationController.sourceView =
                rootController.view;
            controller.popoverPresentationController.sourceRect =
                CGRectMake(
                    CGRectGetMidX(rootController.view.bounds),
                    CGRectGetMidY(rootController.view.bounds),
                    1.0,
                    1.0);
            controller.popoverPresentationController.permittedArrowDirections =
                0;
        }

        [rootController
            presentViewController:controller
            animated:YES
            completion:nil];
    });
}
