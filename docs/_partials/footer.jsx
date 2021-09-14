import React from 'react';

const year = new Date().getFullYear();

export default (
    <div className="content has-text-centered is-size-5">
        <p>
            The content of this website is copyright © {year}
            <br />
            under the terms of the <a className="is-underlined" href="https://github.com/MangelMaxime/Nacara/blob/master/LICENSE.txt" style={{color: "white"}}>MIT License</a>
        </p>
    </div>
)
