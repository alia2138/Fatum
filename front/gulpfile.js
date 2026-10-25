const { src, dest, watch, series } = require("gulp");

const sass = require("gulp-sass")(require("sass"));
const autoprefixer = require("gulp-autoprefixer").default;
const cleanCSS = require("gulp-clean-css");
const sourcemaps = require("gulp-sourcemaps");

function styles() {
    return src("src/scss/style.scss")
        .pipe(sourcemaps.init())
        .pipe(sass().on("error", sass.logError))
        .pipe(autoprefixer())
        .pipe(cleanCSS())
        .pipe(sourcemaps.write("."))
        .pipe(dest("dist/css"));
}

function watcher() {
    watch("src/scss/**/*.scss", styles);
}

exports.default = series(styles, watcher);