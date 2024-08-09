import React from "react";

export const NotFound: React.FC = () => {
    return (
        <div className="container-fluid">
            <div className="text-center">
                <div className="error mx-auto" data-text="404">404</div>
                <p className="lead text-gray-800 mb-5">Page Not Found</p>
                <p className="text-gray-500 mb-0">It looks like you found a glitch in the matrix...</p>
                <a href="/">&larr; Back</a>
            </div>

        </div>
    );
};
