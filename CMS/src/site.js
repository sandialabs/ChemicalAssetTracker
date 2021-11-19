// Write your JavaScript code.

export function is_small_screen() {
    let width = $(document).width();
    let height = $(document).height();

    console.log("Screen site is " + width + " by " + height + "(w x h)");
    return width < 1000;
}

export function go_home() {
    if (is_small_screen()) {
        document.location = "/Home/Index";
    } else {
        //document.location = "/Home/Search";
        document.location = "/Home/Index";
    }
}
