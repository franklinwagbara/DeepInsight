import resolve from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";
import typescript from "@rollup/plugin-typescript";
import terser from "@rollup/plugin-terser";

export default {
  input: "src/index.ts",
  output: [
    {
      file: "dist/deep-insight.umd.js",
      format: "umd",
      name: "DeepInsight",
      sourcemap: true,
    },
    {
      file: "dist/deep-insight.esm.js",
      format: "es",
      sourcemap: true,
    },
  ],
  plugins: [
    resolve({ browser: true }),
    commonjs(),
    typescript({
      tsconfig: "./tsconfig.json",
      declaration: true,
      declarationDir: "dist",
    }),
    terser({
      compress: {
        drop_console: true,
        passes: 2,
      },
      mangle: true,
    }),
  ],
};
