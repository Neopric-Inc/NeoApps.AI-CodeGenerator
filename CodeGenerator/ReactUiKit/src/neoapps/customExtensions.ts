import type { Extensions } from "./extensions";
import { extensions as custom } from "../custom";

/** The app's custom screens, read as Extensions: an untyped `export const extensions = {...}` (common in
 *  assistant-written code) would otherwise be inferred too narrowly and break the build here. */
export const extensions: Extensions = (custom ?? {}) as Extensions;
