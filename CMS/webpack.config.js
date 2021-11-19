const path = require("path");
const VueLoaderPlugin = require("vue-loader/lib/plugin");

module.exports = {
    entry: {
        bundle: "./src/index.js",
        tsbundle: "./src/typescript/TreeViewer.ts",
        testvue: "./src/vue_components/test.vue",
        confirmdialog: "./src/vue_components/confirmdialog.vue",
        infodialog: "./src/vue_components/infodialog.vue",
        itemeditor: "./src/vue_components/itemeditor.vue",
        itemdialog: "./src/vue_components/itemdialog.vue",
        location: "./src/vue_components/location.vue",
        locationselect: "./src/vue_components/locationselect.vue",
        locationpicker: "./src/vue_components/locationpicker.vue",
        textinputdialog: "./src/vue_components/textinputdialog.vue",
        usereditor: "./src/vue_components/usereditor.vue",
        searchsettings: "./src/vue_components/searchsettings.vue",
        grid: "./src/vue_components/grid.vue",
        columnselect: "./src/vue_components/columnselect.vue",
        query: "./src/vue_components/query.vue",
        upload: "./src/vue_components/upload.vue",
        inventory: "./src/vue_components/inventory.vue",
        hazardinfo: "./src/vue_components/hazardinfo.vue",
        barcode: "./src/vue_components/barcode.vue",
        pictogramdialog: "./src/vue_components/pictogramdialog.vue",
        cocdialog: "./src/vue_components/cocdialog.vue",
    },
    externals: {
        jquery: "jQuery",
        vis: "vis",
    },
    mode: "development",
    performance: {
        hints: false,
    },
    output: {
        filename: "[name].js",
        path: path.resolve(__dirname, "wwwroot/dist"),
        pathinfo: true,
    },

    module: {
        rules: [
            {
                test: /\.ts$/,
                exclude: /node_modules|vue\/src/,
                loader: "ts-loader",
            },
            {
                test: /\.vue$/,
                loader: "vue-loader",
            },
            {
                test: /\.m?js$/,
                exclude: /(node_modules|bower_components)/,
                use: {
                    loader: "babel-loader",
                    options: {
                        presets: ["@babel/preset-env"],
                    },
                },
            },
            // this will apply to both plain `.css` files
            // AND `<style>` blocks in `.vue` files
            {
                test: /\.css$/,
                use: ["vue-style-loader", "css-loader"],
            },
        ],
    },
    target: "web",
    plugins: [
        // make sure to include the plugin for the magic
        new VueLoaderPlugin(),
    ],
};
