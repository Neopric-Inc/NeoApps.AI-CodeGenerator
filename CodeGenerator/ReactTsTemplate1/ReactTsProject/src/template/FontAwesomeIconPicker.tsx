import { Edit, Search } from "@mui/icons-material";
import {
    IconButton,
    Popover,
    TextField,
    InputAdornment,
    Divider,
    Skeleton,
} from "@mui/material";
import { useState } from "react";
import { IconName } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "../styles/FontAwesomeIconPicker.scss";
import { useFontAwesomeIconPack } from "./useFontAwesomeIconPack";

export type FontAwesomeIconPickerProps = {
    value?: string;
    index?: number;
    onChange?: (value: string, index?: number) => void;
};

const FontAwesomeIconPicker = ({
    value,
    index,
    onChange,
}: FontAwesomeIconPickerProps) => {
    console.log(value);
    const [searchText, setSearchText] = useState("");
    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);
    const iconPack = useFontAwesomeIconPack();

    if (!iconPack) {
        return <Skeleton variant="rectangular" width={210} height={40} />;
    }

    const iconsFiltered = iconPack.filter((icon) => {
        return icon.iconName.includes(searchText.toLowerCase());
    });

    const handleIconSelect = (iconName: string) => {
        if (onChange) {
            onChange(iconName, index);
        }
    };

    return (
        <>
            <TextField
                fullWidth
                placeholder="Select an icon"
                value={value}
                InputProps={{
                    readOnly: true,
                    endAdornment: (
                        <InputAdornment position="end">
                            <IconButton
                                size="small"
                                onClick={(e) => setAnchorEl(e.currentTarget)}
                            >
                                {value ? (
                                    <FontAwesomeIcon
                                        icon={["fas", value as IconName]}
                                        color="#556ee6"
                                    />
                                ) : (
                                    <Edit fontSize="small" />
                                )}
                            </IconButton>
                        </InputAdornment>
                    ),
                }}
                variant="outlined"
                size="small"
            />
            <Popover
                className="iconPicker"
                id={anchorEl ? "iconPickerPopover" : undefined}
                open={!!anchorEl}
                anchorEl={anchorEl}
                onClose={() => setAnchorEl(null)}
                anchorOrigin={{
                    vertical: "bottom",
                    horizontal: "center",
                }}
                transformOrigin={{
                    vertical: "top",
                    horizontal: "center",
                }}
                PaperProps={{
                    className: "iconPicker__paper",
                }}
            >
                <div className="iconPicker__popoverContainer">
                    <div className="iconPicker__popoverHeader">
                        <TextField
                            fullWidth
                            placeholder="Search"
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <Search />
                                    </InputAdornment>
                                ),
                            }}
                            size="small"
                            variant="outlined"
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                        />
                    </div>
                    <Divider />
                    <div className="iconPicker__iconsContainer">
                        {iconsFiltered.map((icon) => (
                            <div className="iconPicker__iconWrapper" key={icon.iconName}>
                                <button
                                    className={`iconPicker__iconItem ${icon.iconName === value ? "selected" : ""
                                        }`}
                                    title={icon.iconName}
                                    onClick={() => handleIconSelect(icon.iconName)}
                                >
                                    <FontAwesomeIcon icon={icon} />
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            </Popover>
        </>
    );
};
export default FontAwesomeIconPicker;
